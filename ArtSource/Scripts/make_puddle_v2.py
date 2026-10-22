"""Sinh texture vũng nước v2: màu nền + alpha viền mềm, bản đồ phát sáng giả phản chiếu đèn, bản đồ pháp tuyến gợn sóng.
Chạy: python3 ArtSource/make_puddle_v2.py  (hạt giống cố định, chạy lại cho kết quả giống nhau)"""
import numpy as np
from PIL import Image, ImageFilter
N = 1024
rng = np.random.default_rng(20261023)
y, x = np.mgrid[0:N, 0:N].astype(np.float32)
u, v = (x + 0.5) / N * 2 - 1, (y + 0.5) / N * 2 - 1

def noise(scale, seed):
    r = np.random.default_rng(seed)
    g = r.random((scale, scale)).astype(np.float32)
    im = Image.fromarray((g * 255).astype(np.uint8)).resize((N, N), Image.BICUBIC)
    return np.asarray(im, dtype=np.float32) / 255

wob = (noise(6, 1) - .5) * .22 + (noise(14, 2) - .5) * .08
ang = np.arctan2(v, u)
rad = np.sqrt(u * u + v * v) * (1 + wob + 0.04 * np.sin(ang * 3 + 1))
inside = np.clip((0.93 - rad) / 0.10, 0, 1)          # alpha viền mềm
alpha = inside ** 0.8
rim = np.clip(1 - np.abs(rad - 0.86) / 0.05, 0, 1) * (rad < 0.93)   # viền ướt sáng mỏng

depth = np.clip(1 - rad, 0, 1)
base = np.zeros((N, N, 3), np.float32)
base[..., 0] = 0.10 + 0.06 * depth
base[..., 1] = 0.15 + 0.09 * depth
base[..., 2] = 0.24 + 0.14 * depth
base += rim[..., None] * np.array([0.10, 0.10, 0.11], np.float32)
a = np.clip(alpha * 0.78 + rim * 0.12, 0, 1)
rgba = np.dstack([np.clip(base, 0, 1), a])
Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA').save('Assets/TramChanh/Art/Textures/T_Puddle_Water_v2_BaseColor.png')

# phát sáng: vệt sáng dọc ấm/lạnh như phản chiếu đèn đường và bóng đèn quầy
em = np.zeros((N, N, 3), np.float32)
for _ in range(9):
    cx, cy = rng.uniform(-.5, .5), rng.uniform(-.35, .35)
    sx, sy = rng.uniform(.05, .11), rng.uniform(.22, .45)
    warm = rng.random() < .65
    col = np.array([1.0, .62, .25] if warm else [.55, .75, 1.0], np.float32) * rng.uniform(.8, 1.3)
    g = np.exp(-(((u - cx) / sx) ** 2 + ((v - cy) / sy) ** 2))
    em += g[..., None] * col
em *= (noise(24, 5) * .5 + .75)[..., None]          # vỡ nhẹ vì gợn sóng
em *= inside[..., None] ** 2
Image.fromarray((np.clip(em, 0, 1) * 255).astype(np.uint8), 'RGB').save('Assets/TramChanh/Art/Textures/T_Puddle_Water_v2_Emission.png')

# pháp tuyến: gợn sóng đồng tâm tắt dần + nhiễu nhỏ
h = (np.sin(rad * 46 + wob * 18) * np.exp(-rad * 2.2) * 0.5 + (noise(48, 9) - .5) * 0.4 + (noise(120, 10) - .5) * .12)
gx = np.gradient(h, axis=1) * 12
gy = np.gradient(h, axis=0) * 12
nz = np.ones_like(gx)
ln = np.sqrt(gx * gx + gy * gy + nz * nz)
nrm = np.dstack([-gx / ln * .5 + .5, -gy / ln * .5 + .5, nz / ln * .5 + .5])
Image.fromarray((np.clip(nrm, 0, 1) * 255).astype(np.uint8), 'RGB').save('Assets/TramChanh/Art/Textures/T_Puddle_Water_v2_Normal.png')
print('ok')
