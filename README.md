# Lattice – Hack the North 2025 Finalist Winner

Turn a space into a live hologram. 3 Xbox Kinects capture an area from
different angles. Lattice fuses their point clouds into one 3D scene in real
time, and a HoloLens drops that scene in front of you as a hologram you can
interact with.

Won Best Overall and the YC Unicorn Prize demo selection.
[Devpost](https://devpost.com/software/lattice-flck7q)

<img width="500" alt="devpost" src="https://github.com/user-attachments/assets/f77bbfb7-ae00-497c-a7b6-21a34b96ac7c" />

## Layout

The code is split into four main sections

- **LatticeCapture** (C++) is the capture client, one per machine with a
  Kinect v2. It reads depth and color off the sensor, turns each frame into a
  colored point cloud, and talks to the hub over TCP.
- **LatticeHub** (C#) is the server. It takes frames from every connected
  client, merges them into one scene, and renders it in an OpenGL viewport.
  The merged cloud also gets rebroadcast on a second port so other viewers can
  subscribe to it.
- **LatticeAlign** (C++) is a small ICP library the hub calls into to refine
  the alignment between sensors.
- **LatticeReplay** (C#) is a standalone viewer for saved captures.

## How it works

Each client uses the Kinect SDK to map its color frame into 3D, throws away
points that are too close, too far, or outside the capture bounds, and keeps
the rest as a colored point cloud.

Calibration uses printed markers. Each client finds
the marker in its color image with OpenCV, works out where the marker sits
relative to the sensor, and from that gets a transform into a shared world
space. Since every sensor sees the same physical marker, all the clouds land
in the same coordinate system. The pose is cached on disk so restarts skip
recalibration.

Marker calibration only gets the clouds roughly aligned, so the hub runs ICP
on top to tighten it up, aligning every sensor onto the first one that
connected.

Everything on the wire is a compact binary protocol over TCP. The hub sends
commands (grab a frame, calibrate, apply settings) and the clients send frames
and acks back.

The fused scene gets rebroadcast on a second port, and anything on the network
can subscribe to that feed. That's how the AR side works: the HoloLens viewer
we demoed is a separate Unity app that connects to the broadcast port and
renders the cloud as a hologram in front of you. That app lives in its own
repo, this one is the capture and fusion side.
