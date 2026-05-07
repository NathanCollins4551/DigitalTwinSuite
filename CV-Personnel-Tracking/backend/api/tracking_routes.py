from fastapi import APIRouter, Request
from pydantic import BaseModel
from typing import Dict, Optional
import time
import asyncio
from fastapi.responses import StreamingResponse

router = APIRouter()

# In-memory store — updated by CV pipeline
_state = {
    "zone_counts": {
        "Zone_TopLeft":    0,
        "Zone_TopRight":   0,
        "Zone_BottomLeft": 0,
        "Zone_BottomRight":0,
    },
    "updated_at": 0.0,
}

_latest_frame: Optional[bytes] = None

class ZoneCounts(BaseModel):
    zone_counts: Dict[str, int]

@router.post("/tracking/update")
def update_tracking(data: ZoneCounts):
    _state["zone_counts"].update(data.zone_counts)
    _state["updated_at"] = time.time()
    return {"ok": True}

@router.get("/tracking/live")
def get_live():
    return {
        "zone_counts": _state["zone_counts"],
        "updated_at":  _state["updated_at"],
        "stale":       (time.time() - _state["updated_at"]) > 5,
    }

@router.post("/tracking/video_update")
async def update_video(request: Request):
    global _latest_frame
    _latest_frame = await request.body()
    return {"ok": True}

async def frame_generator():
    while True:
        if _latest_frame:
            yield (b'--frame\r\n'
                   b'Content-Type: image/jpeg\r\n\r\n' + _latest_frame + b'\r\n')
        else:
            # Small delay if no frame yet
            await asyncio.sleep(0.1)
            continue
        
        # Limit to ~20 FPS to avoid overloading
        await asyncio.sleep(0.05)

@router.get("/tracking/video_feed")
async def video_feed():
    return StreamingResponse(
        frame_generator(), 
        media_type="multipart/x-mixed-replace; boundary=frame"
    )
