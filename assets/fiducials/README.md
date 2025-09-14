# Fiducial markers

Print these on white paper at the exact side length declared in `FiducialScanner.cpp` (`kTileMm`). The scanner expects a black quiet border with a 4×4 interior code grid.

Place one marker on the floor inside the capture volume before running calibration. Each `LatticeCapture` instance solves for the rigid transform from its own sensor frame to the marker frame; the hub uses that as the initial guess before refinement.
