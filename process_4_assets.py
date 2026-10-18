import os
import sys
import numpy as np
from PIL import Image, ImageFilter

assets = [
    {
        "name": "GlassCase",
        "path": r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1792164845097_f7f76f1e.png",
        "folder": r"c:\git\ProjectDarkRide\UVMapping\GlassCase",
        "maps": [
            ("GlassCase_Albedo.png", "top_left"),
            ("GlassCase_Normal.png", "top_right"),
            ("GlassCase_Roughness.png", "bottom_left"),
            ("GlassCase_Opacity.png", "bottom_right"),
        ]
    },
    {
        "name": "DeerTrophy",
        "path": r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1792164849553_0b9ca36e.png",
        "folder": r"c:\git\ProjectDarkRide\UVMapping\DeerTrophy",
        "maps": [
            ("Deer_Albedo.png", "top_left"),
            ("Deer_Normal.png", "top_right"),
            ("Deer_Roughness.png", "bottom_left"),
            ("Deer_AO.png", "bottom_right"),
        ]
    },
    {
        "name": "StoneWall",
        "path": r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1792164853831_4391c755.jpg",
        "folder": r"c:\git\ProjectDarkRide\UVMapping\StoneWall",
        "maps": [
            ("StoneWall_Albedo.png", "top_left"),
            ("StoneWall_Normal.png", "top_right"),
            ("StoneWall_Roughness.png", "bottom_left"),
            ("StoneWall_AO.png", "bottom_right"),
        ]
    },
    {
        "name": "Sofa",
        "path": r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1792164858244_ee5e949d.png",
        "folder": r"c:\git\ProjectDarkRide\UVMapping\Sofa",
        "maps": [
            ("Sofa_Albedo.png", "top_left"),
            ("Sofa_Normal.png", "top_right"),
            ("Sofa_Roughness.png", "bottom_left"),
            ("Sofa_AO.png", "bottom_right"),
        ]
    }
]

def process_sheet(asset):
    src_path = asset["path"]
    out_dir = asset["folder"]
    os.makedirs(out_dir, exist_ok=True)
    
    if not os.path.exists(src_path):
        print(f"File {src_path} tidak ditemukan!")
        return

    img = Image.open(src_path).convert("RGB")
    W, H = img.size
    arr = np.array(img)
    print(f"\nMemproses {asset['name']} ({W}x{H})...")
    
    # Deteksi kotak Normal Map (warna lavender-blue khas: R~128, G~128, B~255)
    r = arr[:, :, 0]
    g = arr[:, :, 1]
    b = arr[:, :, 2]
    
    norm_mask = (b > 185) & (r > 70) & (r < 195) & (g > 70) & (g < 195)
    # Batasi ke kuadran kanan atas
    norm_mask[int(H * 0.52):, :] = False
    norm_mask[:, :int(W * 0.45)] = False
    norm_mask[:, int(W * 0.88):] = False
    
    y_idx, x_idx = np.where(norm_mask)
    if len(y_idx) > 100:
        nx1, nx2 = int(np.min(x_idx)), int(np.max(x_idx))
        ny1, ny2 = int(np.min(y_idx)), int(np.max(y_idx))
    else:
        # Fallback template coordinate
        nx1, ny1, nx2, ny2 = int(W * 0.51), int(H * 0.14), int(W * 0.85), int(H * 0.48)

    box_w = nx2 - nx1
    box_h = ny2 - ny1
    size = max(box_w, box_h)
    nx2 = nx1 + size
    ny2 = ny1 + size
    
    # Template grid:
    # Top-Left (Albedo):
    gap_x = int(W * 0.028)
    ax2 = nx1 - gap_x
    ax1 = ax2 - size
    ay1 = ny1
    ay2 = ny2
    
    # Bottom Row:
    gap_y = int(H * 0.08)
    by1 = ny2 + gap_y
    by2 = by1 + size
    
    boxes = {
        "top_left": (ax1, ay1, ax2, ay2),
        "top_right": (nx1, ny1, nx2, ny2),
        "bottom_left": (ax1, by1, ax2, by2),
        "bottom_right": (nx1, by1, nx2, by2),
    }
    
    for filename, position in asset["maps"]:
        x1, y1, x2, y2 = boxes[position]
        # Pastikan dalam batas gambar
        x1 = max(0, min(W - 1, x1))
        y1 = max(0, min(H - 1, y1))
        x2 = max(0, min(W, x2))
        y2 = max(0, min(H, y2))
        
        cropped = img.crop((x1, y1, x2, y2))
        # Upscale ke 2048x2048 HD dengan Lanczos
        hd_img = cropped.resize((2048, 2048), Image.Resampling.LANCZOS)
        hd_img = hd_img.filter(ImageFilter.UnsharpMask(radius=2, percent=120, threshold=2))
        
        save_path = os.path.join(out_dir, filename)
        hd_img.save(save_path, "PNG", quality=100)
        
        # Simpan juga ke UVMapping root
        root_path = os.path.join(r"c:\git\ProjectDarkRide\UVMapping", filename)
        hd_img.save(root_path, "PNG", quality=100)
        print(f"  -> Berhasil dibuat: {filename} [2048x2048]")

for a in assets:
    process_sheet(a)

# Sinkronkan ke Unity Assets
print("\nMenyinkronkan ke Unity...")
for root, dirs, files in os.walk(r"c:\git\ProjectDarkRide"):
    if "UVMapping" in dirs:
        unity_uv = os.path.join(root, "UVMapping")
        if unity_uv != r"c:\git\ProjectDarkRide\UVMapping":
            for a in assets:
                dest = os.path.join(unity_uv, a["name"])
                os.makedirs(dest, exist_ok=True)
                for f, _ in a["maps"]:
                    src_f = os.path.join(a["folder"], f)
                    if os.path.exists(src_f):
                        dst_f = os.path.join(dest, f)
                        with open(src_f, "rb") as rf, open(dst_f, "wb") as wf:
                            wf.write(rf.read())
            print(f"  -> Disalin ke: {unity_uv}")

print("\nSELESAI SEMUA! 16 PBR Texture Maps dari 4 Aset Berhasil Dibuat!")
