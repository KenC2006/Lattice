#pragma once

namespace lattice {

struct Vec2f {
    float x;
    float y;
    Vec2f() : x(0.0f), y(0.0f) {}
    Vec2f(float xv, float yv) : x(xv), y(yv) {}
};

}
