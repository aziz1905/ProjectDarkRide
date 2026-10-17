import os
import sys
import numpy as np
from PIL import Image, ImageFilter, ImageEnhance

src_path = r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1791986806013_6d909648.png"
target_dir = r"c:\git\ProjectDarkRide\UVMapping\Character"
os.makedirs(target_dir, exist_ok=True)

if not os.path.exists(src_path):
    print("Source image not found:", src_path)
    sys.exit(1)

img = Image.open(src_path).convert("RGB")
W, H = img.size
arr = np.array(img)

# Deteksi kotak Normal Map (warna lavender-blue: R~128, G~128, B~255)
# di area kanan atas
r = arr[:, :, 0]
g = arr[:, :, 1]
b = arr[:, :, 2]

normal_mask = (b > 200) & (r > 80) & (r < 180) & (g > 80) & (g < 180)
# Batasi ke kuadran kanan atas
normal_mask[int(H * 0.5):, :] = False
normal_mask[:, :int(W * 0.45)] = False

y_indices, x_indices = np.where(normal_mask)
if len(y_indices) == 0:
    print("Normal map not detected, using fallback coordinates")
    norm_x1, norm_y1, norm_x2, norm_y2 = int(W*0.51), int(H*0.16), int(W*0.79), int(H*0.53)
else:
    norm_x1, norm_x2 = int(np.min(x_indices)), int(np.max(x_indices))
    norm_y1, norm_y2 = int(np.min(y_indices)), int(np.max(y_indices))

size = max(norm_x2 - norm_x1, norm_y2 - norm_y1)
norm_x2 = norm_x1 + size
norm_y2 = norm_y1 + size

# Koordinat Albedo (kiri atas)
alb_x2 = norm_x1 - int(W * 0.015)
alb_x1 = alb_x2 - size
alb_y1 = norm_y1
alb_y2 = norm_y2

# Deteksi garis Y kuadran bawah (Roughness & AO)
# AO berada di bawah Normal Map dengan warna putih terang
ao_mask = (r > 220) & (g > 220) & (b > 220)
ao_mask[:int(norm_y2 + 10), :] = False
ao_mask[:, :norm_x1] = False
ao_mask[:, norm_x2:] = False

ao_y, _ = np.where(ao_mask)
if len(ao_y) > 0:
    bot_y1 = int(np.min(ao_y))
else:
    bot_y1 = int(H * 0.58)

bot_y2 = bot_y1 + size

crops = {
    "Character_Albedo.png": (alb_x1, alb_y1, alb_x2, alb_y2, "albedo"),
    "Character_Normal.png": (norm_x1, norm_y1, norm_x2, norm_y2, "normal"),
    "Character_Roughness.png": (alb_x1, bot_y1, alb_x2, bot_y2, "roughness"),
    "Character_AO.png": (norm_x1, bot_y1, norm_x2, bot_y2, "ao"),
}

print(f"Detected 4 UV squares of size {size}x{size}. Upscaling to 2048x2048 (2K HD)...")

for filename, (x1, y1, x2, y2, map_type) in crops.items():
    cropped = img.crop((x1, y1, x2, y2))
    
    # 1. High-quality 2K Resampling (Lanczos)
    hd_2k = cropped.resize((2048, 2048), Image.Resampling.LANCZOS)
    
    # 2. Detail Enhancement & Unsharp Masking
    if map_type == "normal":
        # Normal map butuh presisi vector normal tanpa distorsi warna
        hd_2k = hd_2k.filter(ImageFilter.UnsharpMask(radius=2, percent=120, threshold=2))
    elif map_type in ["albedo", "roughness"]:
        # Albedo & Roughness diberi penajaman tekstur pori kulit & kain celana
        hd_2k = hd_2k.filter(ImageFilter.UnsharpMask(radius=2, percent=160, threshold=2))
        enhancer = ImageEnhance.Sharpness(hd_2k)
        hd_2k = enhancer.enhance(1.25)
    else: # AO
        hd_2k = hd_2k.filter(ImageFilter.UnsharpMask(radius=2, percent=130, threshold=2))
    
    # Simpan ke UVMapping\Character dan UVMapping root
    p1 = os.path.join(target_dir, filename)
    p2 = os.path.join(r"c:\git\ProjectDarkRide\UVMapping", filename)
    
    hd_2k.save(p1, "PNG", quality=100)
    hd_2k.save(p2, "PNG", quality=100)
    print(f"2K HD Saved: {filename} (2048 x 2048 px)")

# Cari folder Unity dan sinkronkan
unity_copied = 0
for root, dirs, files in os.walk(r"c:\git\ProjectDarkRide"):
    if "UVMapping" in dirs:
        dest_folder = os.path.join(root, "UVMapping", "Character")
        if dest_folder != target_dir:
            os.makedirs(dest_folder, exist_ok=True)
            for f in crops.keys():
                src_f = os.path.join(target_dir, f)
                dst_f = os.path.join(dest_folder, f)
                with open(src_f, "rb") as rf, open(dst_f, "wb") as wf:
                    wf.write(rf.read())
            print(f"Synced to Unity: {dest_folder}")
            unity_copied += 1

print(f"\nSELESAI! Seluruh 4 map karakter berhasil ditingkatkan ke resolusi 2K HD (2048x2048)!")
