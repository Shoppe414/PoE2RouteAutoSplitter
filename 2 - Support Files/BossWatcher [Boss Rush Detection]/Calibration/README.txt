PoE2 Boss Watcher calibration files — v0.1.14

Original supplied dual-boss calibration:
  dual_iktab_ekbab_raw.png
  dual_iktab_ekbab_ocr.png

v0.1.13/v0.1.14 lane preprocessing comparison generated from the same raw capture:
  dual_iktab_ekbab_left_gold.png
  dual_iktab_ekbab_left_broad.png
  dual_iktab_ekbab_right_gold.png
  dual_iktab_ekbab_right_broad.png

Using the unchanged v0.1.13/v0.1.14 thresholds and 5x nearest-neighbor upscale, native lane mask counts were:
  LEFT  gold:  472 pixels (3.60%)
  LEFT  broad: 1351 pixels (10.30%)
  RIGHT gold:  471 pixels (3.59%)
  RIGHT broad: 1199 pixels (9.14%)

Offline Tesseract 5.5.0 / PSM 7 sanity check in the build environment:
  LEFT gold  -> poor/noisy recognition
  LEFT broad -> "IKTAB, THEDEATHLORD" with surrounding noise
  RIGHT gold -> blank
  RIGHT broad-> "EKBAB, ANCIENT STEED" with a leading OCR error

The project matcher tolerates the surrounding/leading noise in the broad results. These files are calibration evidence only; live tests remain authoritative.
