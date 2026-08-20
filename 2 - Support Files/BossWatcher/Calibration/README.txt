PoE2 Boss Watcher calibration files — v0.3.7 height-relative geometry

The v0.3.7 dual lane masks below were regenerated with the new wide, midpoint-anchored lane geometry:
  LEFT  = 10%-50% of the centered boss capture
  RIGHT = 50%-90% of the centered boss capture

The outer live boss capture is now centered and width-scaled from client height. At 16:9 this exactly reproduces the historical 50%-of-client-width capture; on ultrawide/super-ultrawide it no longer expands with total client width.

Existing Iktab/Ekbab raw calibration (legacy captured ROI, retained as source evidence):
  dual_iktab_ekbab_raw.png
  dual_iktab_ekbab_ocr.png

v0.3.7 lane preprocessing regenerated from that raw capture:
  dual_iktab_ekbab_left_gold.png
  dual_iktab_ekbab_left_broad.png
  dual_iktab_ekbab_right_gold.png
  dual_iktab_ekbab_right_broad.png

Using the unchanged thresholds and 5x nearest-neighbor upscale, native wide-lane mask counts are:
  LEFT  gold:  472 pixels (1.80%)
  LEFT  broad: 1862 pixels (7.10%)
  RIGHT gold:  471 pixels (1.80%)
  RIGHT broad: 1317 pixels (5.03%)

Offline Tesseract 5.5.0 / PSM 7 sanity check in this validation environment:
  LEFT broad  -> contains "IKTAB, THE DEATHLORD" with surrounding noise
  RIGHT broad -> contains "EKBAB, ANCIENT STEED" with surrounding noise

The project matcher is responsible for tolerating surrounding OCR noise. These files are calibration evidence only; live tests remain authoritative.
