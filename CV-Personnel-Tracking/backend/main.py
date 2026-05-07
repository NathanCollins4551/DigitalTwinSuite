from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from backend.api.health_routes import router as health_router
from backend.api.inventory_routes import router as inventory_router
from backend.api.event_routes import router as event_router
from backend.api.tracking_routes import router as tracking_router
from backend.core.logging import setup_logging

setup_logging()

app = FastAPI(
    title="Inventory Tracking Backend (Phase 1)",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:3000",
        "http://localhost:5173",
        "*"
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(health_router)
app.include_router(inventory_router, prefix="/api")
app.include_router(event_router, prefix="/api")
app.include_router(tracking_router, prefix="/api")