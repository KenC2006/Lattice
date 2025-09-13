# Lattice — Hack the North 2025 Finalist Winner  

**🏆 Awards:**  
- **Best Overall**  
- **YC Unicorn Prize Interview Selection**

**Event:** Hack the North 2025  
**Date:** September 2025  

## Overview  

**Lattice** is a **real-time holographic projection framework** that captures 3D volumetric data using **Xbox Kinect depth cameras** and renders fully aligned, live 3D reconstructions for **HoloLens** telepresence visualization.  

The system enables users to experience immersive holographic presence — turning multi-view depth captures into coherent, real-world 3D scenes streamed instantly between physical and mixed-reality environments.

## Technical Architecture  

### 1. Multi-Sensor RGB-D Capture  
Lattice utilizes **three Xbox Kinect v2 cameras**, each calibrated with intrinsic parameters for accurate RGB-D mapping. The cameras capture synchronized frames containing both **depth (D)** and **color (RGB)** data streams.  
- Each frame is converted into a **point cloud** via per-pixel projection using the camera’s internal pinhole model:
P = K^-1 * [u, v, 1]^T * D(u,v)
where `K` is the intrinsic matrix, and `D(u,v)` is the depth value.  
- The corresponding color value is assigned per point for photorealistic rendering.

### 2. Real-Time Network Streaming  
Captured point clouds are serialized and **streamed asynchronously to a central server** using WebSocket connections.  
- Each packet includes: timestamp, camera ID, RGB-D data, and transformation metadata.  
- A custom binary protocol minimizes overhead and preserves spatial fidelity.

### 3. Calibration and Point Cloud Fusion  
The server performs **multi-sensor spatial calibration** using:  
- **Iterative Closest Point (ICP)** to compute rigid body transformations aligning each camera’s frame to a common global coordinate system.  
- **Convex hull enclosure** to maintain consistent spatial boundaries and prevent ghosting between overlapping clouds.  

Real-time transforms are applied to every incoming point cloud frame to ensure geometric alignment across views.

### 4. Temporal Synchronization and Filtering  
To maintain smooth motion capture and reduce jitter:  
- Frames are **timestamp-synchronized** within ±5 ms tolerance.  
- Overlapping regions are merged through **spatial averaging and bilateral filtering**, removing sensor noise while preserving edges.

### 5. Holographic Visualization  
The final unified 3D scene is transmitted to the **HoloLens client**, where the user can view the live reconstructed hologram in full 3D space.  
- Rendering uses **Unity3D** with GPU-accelerated shaders for point-based rendering.  
- The framework supports both **local rendering** and **remote streaming** modes for telepresence applications.

## Tech Stack  

| Component | Technology |
|------------|-------------|
| **Capture** | Xbox Kinect SDK, OpenCV, C++ |
| **Networking** | WebSocket (Boost Asio), Protobuf serialization |
| **Server Fusion** | C++, PCL (Point Cloud Library), Eigen |
| **Visualization** | Unity3D, HoloLens SDK, C# |
| **Infrastructure** | UDP transport layer, asynchronous threading, timestamp synchronization |

## Key Features  
- Multi-Kinect RGB-D fusion with real-time calibration  
- Low-latency network streaming with timestamp alignment  
- Robust 3D point cloud merging using ICP  
- Live holographic rendering on Microsoft HoloLens  

## Devpost  
<img width="500" height="500" alt="image" src="https://github.com/user-attachments/assets/f77bbfb7-ae00-497c-a7b6-21a34b96ac7c" />

*[Devpost link](https://devpost.com/software/lattice-flck7q)*  



