#pragma once

#ifdef LATTICE_ALIGN_BUILD
#  define LATTICE_API __declspec(dllexport)
#else
#  define LATTICE_API __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

LATTICE_API float __stdcall lattice_align(
    const float* sourceXYZ, int sourceCount,
    const float* targetXYZ, int targetCount,
    float* outRotation,
    float* outTranslation,
    int maxIterations,
    float trimRatio);

#ifdef __cplusplus
}
#endif
