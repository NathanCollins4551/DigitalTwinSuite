import json
import socket
from typing import List, Optional


class UnityPublisher:
    """
    Sends tracked object state to Unity via UDP each frame.

    Payload format (JSON):
    {
      "objects": [
        {
          "id": 1,
          "label": "person",
          "x": 0.35,      // normalized 0..1 (left -> right)
          "y": 0.60,      // normalized 0..1 (top -> bottom)
          "zone": "Zone_Left"   // null if outside all zones
        },
        ...
      ]
    }
    """

    def __init__(self, host: str, port: int, frame_width: int, frame_height: int):
        self.host = host
        self.port = port
        self.frame_width = frame_width
        self.frame_height = frame_height
        self._sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def publish(self, tracks: List[dict]):
        objects = []
        for t in tracks:
            # Skip ghost tracks — only send objects actively detected this frame
            if not t.get("detected", True):
                continue
            x1, y1, x2, y2 = t["bbox"]
            cx = (x1 + x2) / 2.0
            cy = (y1 + y2) / 2.0
            objects.append({
                "id": t["track_id"],
                "label": t["label"],
                "x": round(cx / self.frame_width, 4),
                "y": round(cy / self.frame_height, 4),
                "zone": t.get("zone_id"),
            })

        payload = json.dumps({"objects": objects}).encode("utf-8")
        try:
            self._sock.sendto(payload, (self.host, self.port))
        except OSError:
            pass  # non-blocking; drop silently if Unity is not listening

    def close(self):
        self._sock.close()
