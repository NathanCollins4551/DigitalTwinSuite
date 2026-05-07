import cv2
import time

# BGR colors per zone
# Zone 1 (TL): Green
# Zone 2 (TR): Blue
# Zone 3 (BL): Cyan
# Zone 4 (BR): Red (RESTRICTED)
_ZONE_COLORS = {
    "Zone_TopLeft":    (80,  200,  80),   # green
    "Zone_TopRight":   (200, 130,  50),   # blue
    "Zone_BottomLeft": (230, 170,  50),   # cyan/light-blue
    "Zone_BottomRight":(50,  50,  230),   # red — RESTRICTED
}

_ZONE_LABELS = {
    "Zone_TopLeft":    "Zone 1",
    "Zone_TopRight":   "Zone 2",
    "Zone_BottomLeft": "Zone 3",
    "Zone_BottomRight":"Zone 4 - RESTRICTED",
}

_DEFAULT_ZONE_COLOR = (255, 255, 255)

def _zone_color(zone_id):
    return _ZONE_COLORS.get(zone_id, _DEFAULT_ZONE_COLOR)

def _zone_label(zone_id):
    return _ZONE_LABELS.get(zone_id, zone_id)

def draw_rect_zone(frame, zone, color=None, thickness=2):
    x1, y1, x2, y2 = zone["x1"], zone["y1"], zone["x2"], zone["y2"]
    zid = zone["zone_id"]
    
    # Force red for restricted zone 4
    c = color if color is not None else _zone_color(zid)
    label = _zone_label(zid)

    # Semi-transparent fill
    overlay = frame.copy()
    cv2.rectangle(overlay, (x1, y1), (x2, y2), c, -1)
    
    # More opaque for restricted area
    alpha = 0.15 if zid == "Zone_BottomRight" else 0.08
    cv2.addWeighted(overlay, alpha, frame, 1 - alpha, 0, frame)

    # Border
    cv2.rectangle(frame, (x1, y1), (x2, y2), c, thickness)

    # Label
    cv2.putText(frame, label, (x1 + 8, y1 + 28),
                cv2.FONT_HERSHEY_SIMPLEX, 0.8, c, 2, cv2.LINE_AA)

def draw_bbox(frame, bbox, label, conf, color=(255, 255, 255)):
    x1, y1, x2, y2 = bbox
    cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
    cv2.putText(frame, f"{label} {conf:.2f}", (x1, max(0, y1 - 10)),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2, cv2.LINE_AA)

def draw_zone_counts(frame, zones, counts):
    """Overlay person count inside each zone rectangle."""
    for z in zones:
        count = counts.get(z["zone_id"], 0)
        x1, y1 = z["x1"], z["y1"]
        # Match the zone color for the count text
        c = _zone_color(z["zone_id"])
        cv2.putText(frame, f"Personnel: {count}", (x1 + 8, y1 + 60),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, c, 2, cv2.LINE_AA)

def draw_warning_zone(frame, zone):
    """Draw a flashing red border and warning banner on a no-enter zone."""
    x1, y1, x2, y2 = zone["x1"], zone["y1"], zone["x2"], zone["y2"]

    # Flash by alternating thickness based on time (0.5s on/off)
    flash_on = int(time.time() * 2) % 2 == 0
    color = (0, 0, 255)
    thickness = 6 if flash_on else 3
    cv2.rectangle(frame, (x1, y1), (x2, y2), color, thickness)

    # Red semi-transparent overlay
    overlay = frame.copy()
    cv2.rectangle(overlay, (x1, y1), (x2, y2), (0, 0, 180), -1)
    cv2.addWeighted(overlay, 0.25, frame, 0.75, 0, frame)

    # Warning banner at top of zone
    banner_h = 40
    cv2.rectangle(frame, (x1, y1), (x2, y1 + banner_h), (0, 0, 200), -1)
    cv2.putText(frame, "!! RESTRICTED AREA — UNAUTHORIZED ENTRY !!",
                (x1 + 10, y1 + 30), cv2.FONT_HERSHEY_SIMPLEX, 0.65, (255, 255, 255), 2, cv2.LINE_AA)

def draw_transfer_log(frame, transfer_counts):
    """Overlay zone-to-zone transfer history in the bottom-left corner."""
    if not transfer_counts:
        return
    h = frame.shape[0]
    entries = sorted(transfer_counts.items())
    y = h - 20 - (len(entries) * 22)
    cv2.putText(frame, "Movement History:", (10, y),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 200, 255), 2, cv2.LINE_AA)
    y += 26
    for (from_z, to_z), count in entries:
        # Convert internal IDs to "Zone X" for the log
        from_label = _zone_label(from_z).split(' - ')[0]
        to_label = _zone_label(to_z).split(' - ')[0]
        cv2.putText(frame, f"  {from_label} -> {to_label}: {count}",
                    (10, y), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (0, 200, 255), 1, cv2.LINE_AA)
        y += 22
