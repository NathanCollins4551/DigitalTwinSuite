import json
import time
import threading
import pika
from typing import Any, Dict, Optional

class RabbitMQPublisher:
    def __init__(
        self,
        host: str,
        port: int,
        username: str,
        password: str,
        exchange: str = "cv.events",
        exchange_type: str = "topic",
        vhost: str = "/",
        enable_confirm: bool = False,
    ):
        self.host = host
        self.port = int(port)
        self.username = username
        self.password = password
        self.vhost = vhost
        self.exchange = exchange
        self.exchange_type = exchange_type
        self.enable_confirm = enable_confirm

        self.conn: Optional[pika.BlockingConnection] = None
        self.ch: Optional[pika.adapters.blocking_connection.BlockingChannel] = None
        self._lock = threading.Lock()
        self._is_connecting = False
        
        # Start connection in background so we don't block the main CV loop
        threading.Thread(target=self._connect, daemon=True).start()

    def _connect(self):
        with self._lock:
            if self._is_connecting:
                return
            self._is_connecting = True

        try:
            creds = pika.PlainCredentials(self.username, self.password)
            params = pika.ConnectionParameters(
                host=self.host,
                port=self.port,
                virtual_host=self.vhost,
                credentials=creds,
                heartbeat=30,
                blocked_connection_timeout=30,
                connection_attempts=3,
                retry_delay=2,
            )
            
            # Add a more robust retry loop for the initial connection
            max_retries = 10
            for i in range(max_retries):
                try:
                    new_conn = pika.BlockingConnection(params)
                    new_ch = new_conn.channel()
                    new_ch.exchange_declare(exchange=self.exchange, exchange_type=self.exchange_type, durable=True)

                    if self.enable_confirm:
                        new_ch.confirm_delivery()
                    
                    with self._lock:
                        self.conn = new_conn
                        self.ch = new_ch
                        self._is_connecting = False
                    
                    print(f"Successfully connected to RabbitMQ at {self.host}:{self.port}")
                    return
                except (pika.exceptions.AMQPConnectionError, Exception) as e:
                    if i < max_retries - 1:
                        wait_time = 5
                        print(f"RabbitMQ at {self.host}:{self.port} not ready or DNS failure ({e}). Retrying in {wait_time}s... ({i+1}/{max_retries})")
                        time.sleep(wait_time)
                    else:
                        print(f"Failed to connect to RabbitMQ after {max_retries} attempts.")
                        with self._lock:
                            self._is_connecting = False
                        raise e
        except Exception:
            with self._lock:
                self._is_connecting = False

    def publish(self, routing_key: str, message: Dict[str, Any]) -> None:
        with self._lock:
            if not self.conn or self.conn.is_closed or not self.ch or self.ch.is_closed:
                if not self._is_connecting:
                    threading.Thread(target=self._connect, daemon=True).start()
                return # Skip publishing if not connected yet

        body = json.dumps(message, separators=(",", ":"), ensure_ascii=False).encode("utf-8")

        try:
            self.ch.basic_publish(
                exchange=self.exchange,
                routing_key=routing_key,
                body=body,
                properties=pika.BasicProperties(
                    content_type="application/json",
                    delivery_mode=2,
                ),
            )
        except Exception:
            self._safe_close()

    def _safe_close(self):
        try:
            if self.ch and not self.ch.is_closed:
                self.ch.close()
        except Exception:
            pass
        try:
            if self.conn and not self.conn.is_closed:
                self.conn.close()
        except Exception:
            pass

    def close(self):
        self._safe_close()