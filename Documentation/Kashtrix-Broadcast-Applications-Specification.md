# Kashtrix Broadcast Playout Suite — Complete System Architecture & Application Specification

**Document Version:** FIX49H Production Build Master Specification  
**Platform Target:** Windows 10 / 11 / Windows Server 2022/2025 (x64 Native)  
**Technology Stack:** .NET 8.0 / .NET 9.0 Desktop (WPF, Direct3D11, Direct2D/DirectWrite, DirectShow, Media Foundation, Native FFmpeg 7+, Native NDI 6, Blackmagic DeckLink SDK, AJA Kona SDK)  
**Workspace Repository:** `C:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H`  
**Solution File:** `BroadcastPlayout.sln`  

---

## 1. Executive System Overview

The **Kashtrix Broadcast Playout Suite** is an enterprise-grade broadcast television automation, master control playout (MCR), real-time motion character generator (CG), newsroom computer system (NRCS), media asset management (MAM), and multi-protocol IP/SDI broadcast transmission infrastructure.

Engineered for 24/7 continuous on-air transmission with zero downtime tolerance, the suite is partitioned into **16 standalone native Windows applications** and **3 shared infrastructure subsystems**. Each application operates either as a dedicated workstation surface (e.g., in MCR, newsroom, graphics desk, traffic department, or studio gallery) or as an autonomous headless background engine (e.g., playout server nodes, ingest encoders, transmission stream matrix, and high-availability arbitration daemons).

### 1.1 Architecture & Design Tenets

1. **Multi-Process Decoupled Resiliency:**
   - Critical transmission pipelines (`BroadcastPlayout.App`, `Kashtrix.OutputEngine`, `Kashtrix.IngestServer`) execute in isolated OS processes. A failure in an operator GUI, third-party plugin, or secondary graphics surface will never interrupt active broadcast transmission.
2. **Zero-Copy GPU Accelerated Pipeline:**
   - Direct3D 11 surface sharing and hardware-accelerated video decoding (NVDEC, Intel QuickSync Video / QSV, DXVA2, D3D11VA) feed directly into hardware render targets, eliminating CPU-RAM-GPU round-tripping for 1080i50, 1080p50/59.94, and 4K UHD 2160p rasters.
3. **Sub-Frame Deterministic Timing:**
   - Playout clocking is synchronized to SMPTE 12M linear timecode (LTC), vertical interval timecode (VITC), Precision Time Protocol (IEEE 1588 PTP), and high-precision system multimedia timers (Multimedia Timer API with 1ms resolution).
4. **Unified Inter-Process Communication (IPC) & Platform Bus:**
   - Applications exchange state, rundown events, graphics commands, and health metrics via the **Kashtrix Unified Platform Control Bus** (in-memory circular bus, local TCP JSONL, real-time WebSockets, UDP triggers, and zero-copy shared memory channels).
5. **Universal Media & Ancillary Data Interoperability:**
   - Native ingest, transcode, and playout support for broadcast codecs (Apple ProRes 422/4444, Avid DNxHD/DNxHR, Sony XDCAM HD422 MXF OP1a, Panasonic AVC-Intra, D10/IMX, MPEG-2 TS/PS, H.264, and H.265/HEVC).
   - Real-time VANC metadata insertion: CEA-608 / CEA-708 closed captions, OP-47 / DVB subtitles, SMPTE 2010 SCTE-35 digital ad insertion markers, and DVB SI/PSI descriptor tables.

---

## 2. Global Solution Map & Architectural Topology

```
Kashtrix Broadcast Playout Suite (BroadcastPlayout.sln)
├── [Master Playout & Core Engines]
│   ├── 1. BroadcastPlayout.App          --> Master Control Playout, Compositor & Channel Automation
│   ├── 2. Kashtrix.OutputEngine         --> Multi-Channel Hardware SDI & IP Transmission Matrix
│   └── 11. Kashtrix.InputEngine         --> Live Baseband SDI/HDMI, SRT/NDI Receiver & Frame Sync Hub
│
├── [Graphics & Character Generation (CG)]
│   ├── 3. Kashtrix.CGEditor             --> Visual Motion CG, Vector Timeline & Template Studio
│   └── 4. Kashtrix.CGController         --> Live On-Air Multi-Layer Graphics Play-out Console
│
├── [Automation, Routing & Scheduling]
│   ├── 5. Kashtrix.ChannelController    --> Multi-Channel Supervision & Hardware Router Matrix
│   ├── 6. Kashtrix.PlaylistEditor       --> Broadcast Rundown, Multi-Part Trimming & Log Manager
│   └── 7. Kashtrix.Scheduler            --> Long-Range Programming Grid, Ad Traffic & As-Run Reconciliation
│
├── [Asset Management & Fast Ingest]
│   ├── 8. Kashtrix.MAM                  --> Enterprise Media Asset Management & Multi-Tier HSM
│   ├── 9. Kashtrix.FileManager          --> Media Explorer, Hot-Folder Watcher & Normalization Ingest
│   ├── 10. Kashtrix.IngestServer        --> Baseband/IP Ingest, Growing File & Compliance Recorder
│   └── 12. Kashtrix.QCController        --> Automated Pre-Broadcast QC & EBU R128 Loudness Inspector
│
├── [Newsroom & Studio Production]
│   ├── 13. Kashtrix.Multiview           --> Broadcast Multi-Tile Mosaic Wall & Signal Alert Monitor
│   ├── 14. Kashtrix.NRCS                --> Newsroom Computer System (MOS 2.8.4) & Dual Calendar Desk
│   └── 15. Kashtrix.Prompter            --> Sub-Pixel Studio Teleprompter & Anchor Display Engine
│
├── [Redundancy & High Availability]
│   └── 16. Kashtrix.HAController        --> 1+1 Hot Standby, N+M Cluster Coordinator & Output Interlock
│
└── [Underlying System Infrastructure]
    ├── Kashtrix.ApiGateway              --> Unified API (REST, WebSocket, TCP 9090, UDP 9091, MCP /mcp)
    ├── Kashtrix.Settings                --> System-Wide Hardware, Standards & Video Processing Config Hub
    └── Kashtrix.Shared                  --> Common Math, SMPTE 12M, SCTE-35, DVB SI/PSI & UI Framework
```

---

## 3. Detailed Technical Breakdown: The 16 Applications

---

### Application 1: `BroadcastPlayout.App` — Master Control Playout & Compositor Engine

- **Executable:** `BroadcastPlayout.App.exe`
- **Source Project:** `src/BroadcastPlayout.App/BroadcastPlayout.App.csproj`
- **Primary Operational Role:** 24/7 master control playout engine delivering continuous, glitch-free broadcast video, audio, dynamic graphics, and secondary event automation.

#### 1. Architecture & Rendering Pipelines
- **Dual Independent Video Pipelines (PGM & PVW):**
  - **Program (PGM) Pipeline:** Real-time presentation surface driving master broadcast outputs. Operates under strict hardware V-Sync locked clocks (50.00 Hz, 59.94 Hz, or 60.00 Hz).
  - **Preview (PVW) Pipeline:** Frame-accurate secondary rendering pipeline allowing operators to audition upcoming clips, verify graphics bindings, inspect trim points, and review multi-part segment boundaries before committing to air.
- **Hardware-Accelerated Zero-Copy Compositor:**
  - Direct3D 11 rendering pipeline with hardware alpha blending (pre-multiplied and straight alpha).
  - Native hardware decoding offload: NVIDIA NVDEC (CUDA), Intel QuickSync Video (QSV / D3D11VA), AMD AMF, and Microsoft DXVA2.
  - Multi-threaded FFmpeg 7+ native decoding backend for non-accelerated esoteric formats.
- **Supported Video Codecs & Containers:**
  - *Broadcast Mastering:* Apple ProRes (422, 422 HQ, 422 LT, 422 Proxy, 4444 with 32-bit alpha), Avid DNxHD / DNxHR (HQX, SQ, LB), Sony XDCAM HD422 (MXF OP1a 50 Mbps), Sony D10 / IMX (30/40/50 Mbps), Panasonic AVC-Intra 50/100/200, DVCPRO HD.
  - *Distribution & Web:* H.264 / AVC (High 4:2:2, High 10, Main), H.265 / HEVC (Main, Main 10), MPEG-2 Broadcast Transport & Program Streams (`.ts`, `.mpg`), WebM (VP8, VP9 with full alpha channel), QuickTime Animation (RLE), PNG image sequences.

#### 2. Transitions & Mixing Engine
- **Seamless Video Transitions:**
  - Real-time Cut, Cross-Dissolve (Fade), Horizontal/Vertical Wipes, Sliding Pushes, and Smooth Squeeze/DVE moves.
  - Transition durations customizable from 1 frame up to 500 frames (or 10ms to 10,000ms).
- **Audio Auto-Follow-Video (AFV) Mixing:**
  - Program audio automatically crossfades with video transitions using user-selectable linear, equal-power (3 dB), or logarithmic curves.
  - Independent channel audio ducking: Voice-over or live presenter inputs automatically attenuate background music/clip audio by a user-defined threshold (-6 dB to -30 dB).

#### 3. Real-Time Graphics Compositor (DSK Engine)
- **Multi-Layer Direct3D Graphic Overlay Stack:**
  - Up to 8 concurrent on-air graphics layers rendered simultaneously without impacting video decode framerates:
    - *Layer 1:* Station Identification Bug / Permanent Watermark (with animated reveal).
    - *Layer 2:* Continuous Multi-Topic News Ticker / Crawl (smooth sub-pixel scrolling).
    - *Layer 3:* Lower Thirds, Guest Identifiers, Location Straps.
    - *Layer 4:* Now / Next / Later Promotional Snipes and Squeeze-Back DVE.
    - *Layer 5:* Live Sports Bug / Scoreboard / Match Statistics.
    - *Layer 6:* Live Weather Alert Banners and Temperature Badges.
    - *Layer 7:* Emergency Alert System (EAS) Full-Screen / Banner Crawls.
    - *Layer 8:* Downstream Keyer (DSK) Master Emergency Slate.
- **Dynamic Variable In-Flight Replacement:**
  - Text values, colors, image URLs, and scoreboard data fields can be modified while a graphic is live on air without restarting the entrance animation.

#### 4. Master Audio Engine & Audio Mixer
- **Multi-Track Audio Architecture:**
  - Discrete multi-channel routing supporting up to 16 audio channels per clip.
  - Dedicated `AudioMixerWindow.xaml`: 8-channel stereo pair faders, master output trim slider (0% to 200%), master mute switch, master mono fold-down, and per-channel delay compensation (0ms to 2000ms).
  - Program vs. Preview audio routing: Operators audition cued items on dedicated control-room monitor headphones without altering master transmission audio.
- **Real-Time Loudness Monitoring:**
  - Continuous EBU R128 and ITU-R BS.1770-4 compliance metering: Integrated LUFS, Loudness Range (LRA), Momentary (400ms), Short-Term (3s), and True Peak (dBTP).

#### 5. Rundown Grid & Event Execution Modes
- **Broadcast Execution Timing Policies:**
  - **Auto-Follow:** Seamless back-to-back clip triggering with zero black frames between events.
  - **Manual Hold / Cue:** Freezes on the final frame of the clip (or holds cued item) until the operator presses TAKE / PLAY.
  - **Hard Start (Time-Locked):** Synchronized to national broadcast wall-clock (e.g., National News triggers at exactly 20:00:00:00, preempting any running filler).
  - **Soft Start:** Waits for the currently playing item to finish before launching the scheduled time block.
  - **Join-In-Progress (JIP):** Calculates elapsed time of a live event and joins mid-stream at the mathematically calculated offset.
- **Multi-Part Media Playback:**
  - A single master media file can be divided into multiple commercial segments (e.g., Part 1: 00:00 to 14:30, Part 2: 15:30 to 28:00) within one playlist row without rendering intermediate files.

#### 6. Secondary Event Automation
- **SCTE-35 Digital Ad Insertion (DPI):** Automated injection of Splice In / Splice Out markers, Time Signals, and Segmentation Descriptors timed to commercial breaks.
- **Downstream Keyer (DSK) Triggers:** Automatically arms station bugs and promos based on clip category (e.g., suppress bug during commercials; show bug during movies).
- **Router / Crosspoint Control:** Fires IP or serial commands to external matrix routers (Videohub, Ross, Grass Valley) on live input playlist events.

#### 7. Operator UI Windows & Modals
- `MainWindow.xaml`: Primary MCR operational console (Dual PGM/PVW monitors, rundown grid, countdown clocks, audio meters, transport controls).
- `AudioMixerWindow.xaml`: Studio faders, routing matrix, and loudness meters.
- `MediaTrimmerWindow.xaml`: Precision non-destructive in/out and multi-part editor.
- `PlaylistItemPropertiesWindow.xaml`: Event timing, audio mapping, secondary CG attachments, and SCTE parameters.
- `BroadcastDynamicFieldsDialog.xaml`, `NowNextFieldsDialog.xaml`, `WeatherFieldsDialog.xaml`: Fast live text entry.
- `EngineDashboardWindow.xaml`: Real-time Direct3D frame timings, memory usage, and decode performance.
- `AuditLogWindow.xaml`: Chronological record of all operator actions and system alerts.

---

### Application 2: `Kashtrix.OutputEngine` — Transmission & Multi-Protocol Output Matrix

- **Executable:** `Kashtrix.OutputEngine.exe`
- **Source Project:** `src/Kashtrix.OutputEngine/Kashtrix.OutputEngine.csproj`
- **Primary Operational Role:** Multi-channel broadcast transmission and digital streaming matrix delivering playout video to physical SDI/HDMI hardware, uncompressed studio IP networks, DVB modulators, and internet CDNs.

#### 1. Baseband Hardware Broadcast Output
- **Blackmagic Design DeckLink & UltraStudio:**
  - 12G/6G/3G/HD/SD-SDI and HDMI output via Blackmagic Desktop Video SDK.
  - Support for DeckLink Duo 2, Quad 2, 8K Pro, Studio 4K, and UltraStudio 4K Mini.
  - Up to 16 channels of embedded 24-bit 48 kHz PCM audio.
  - Tri-level and Blackburst reference genlock synchronization.
  - Hardware VANC packet insertion (SMPTE 2010 SCTE-35, CEA-608/708 captions, and SMPTE 12M timecode).
- **AJA Kona & Corvid Series:**
  - Low-latency master baseband transmission with multi-channel SDI audio embedding and reference lock.
- **Matrox DSX Series:**
  - Hardware-accelerated enterprise video I/O boards for Tier-1 transmission facilities.
- **Virtual DirectShow Driver (`.ax` filter):**
  - Exposes playout output as a virtual Windows DirectShow camera device (`Kashtrix Virtual Output`) for consumption by Zoom, Teams, OBS Studio, and vMix.

#### 2. Uncompressed & Low-Latency IP Studio Protocols
- **Native NDI 6 Integration:**
  - Full 32-bit RGBA video stream with uncompressed alpha channel (Fill & Key over single network connection).
  - Bi-directional Tally signaling (Program tally turns UI red; Preview tally turns UI green).
  - NDI Discovery Server integration for enterprise subnets and VLAN routing.
- **SMPTE ST 2110 / ST 2022-6 Infrastructure:**
  - SMPTE ST 2110-20 (Uncompressed Video), ST 2110-30 (PCM Audio), ST 2110-40 (Ancillary Data).
  - SMPTE ST 2022-7 Seamless Hitless Protection Switching (Redundant network path A and path B).
  - NMOS IS-04 (Discovery & Registration) and NMOS IS-05 (Device Connection Management).

#### 3. DVB Digital Television Transport Streams
- **DVB-TS UDP (Unicast & Multicast):**
  - Fully compliant MPEG-2 Transport Stream generation for DVB-C (Cable), DVB-T/T2 (Terrestrial), and DVB-S/S2 (Satellite) modulators.
  - Configurable UDP Multicast IP (`224.0.0.0/4` - e.g., `239.255.0.1:1234`) with Time-To-Live (TTL) and Network Interface selection.
- **DVB SI/PSI Table Generator Engine:**
  - Injects Program Association Table (PAT), Program Map Table (PMT), Service Description Table (SDT), Network Information Table (NIT), and Time and Date Table (TDT/TOT).
  - User-configurable Service ID, PMT PID, Video PID, Audio PID, and PCR PID.
- **High-Precision PCR Clocking:**
  - Program Clock Reference (PCR) packet interval clocked to ≤ 20ms with low jitter, preventing buffer underflows or overflows in consumer set-top boxes (STBs).

#### 4. SCTE-35 Splice Signaling Engine
- Real-time injection of SCTE-35 binary cue packets into the DVB-TS stream:
  - `splice_insert` commands with designated splice event IDs, duration ticks (90 kHz), auto-return flags, and cancellation commands.
  - `time_signal` with Segmentation Descriptors conforming to SCTE 35 2019/2020 for regional commercial replacement and OTT dynamic ad insertion (DAI).

#### 5. Internet OTT & CDN Streaming
- **SRT (Secure Reliable Transport):**
  - Modes: Caller, Listener, and Rendezvous.
  - End-to-end AES-128 and AES-256 stream encryption with pre-shared passphrase.
  - Configurable SRT Latency buffer (20ms to 8000ms) with automated ARQ packet loss recovery across public internet links.
- **RTMP / RTMPS Multi-Destination Streaming:**
  - Direct transmission to YouTube Live, Facebook Live, Twitch, Kick, TikTok, and custom CDN ingestion endpoints.
- **Direct HLS & MPEG-DASH / MPD Packaging:**
  - On-the-fly chunking (TS/fMP4 segments) and m3u8/mpd manifest generation for web streaming without external media servers.
- **RTSP Server:**
  - Low-latency surveillance, internal confidence feeds, and IPTV distribution.

#### 6. Multi-Vendor GPU Video Encoders
- **NVIDIA NVENC:** H.264 & HEVC/H.265 hardware encoding using NVCODEC API (CBR, VBR, HQ Low-Latency tuning, Lookahead, B-Frames).
- **Intel QuickSync Video (QSV):** Hardware-accelerated AVC/HEVC encoding offloading CPU completely.
- **AMD AMF:** Hardware encoding support for Radeon Pro and Instinct enterprise GPUs.
- **Software Fallback:** Multi-threaded CPU encoding via libx264, libx265, and libvpx.

#### 7. Dialogs & UI Surfaces
- `MainWindow.xaml`: Output status wall, active bitrates, FPS meters, dropped packet counters, and health status.
- `AddOutputDialog.xaml`: Multi-protocol stream configuration (device selection, raster, FPS, encoder type, bitrates, PIDs, SCTE-35 toggles).
- `InspectOutputDialog.xaml`: Deep stream diagnostics, encoder buffer occupancy, and network jitter graphs.

---

### Application 3: `Kashtrix.CGEditor` — Visual Motion CG & Graphic Template Designer

- **Executable:** `Kashtrix.CGEditor.exe`
- **Source Project:** `src/Kashtrix.CGEditor/Kashtrix.CGEditor.csproj`
- **Primary Operational Role:** Standalone professional graphic design studio for authoring animated broadcast templates, lower thirds, news tickers, sports scoreboards, channel bugs, and election packages.

#### 1. Multi-Track Non-Linear Animation Timeline
- **Frame-by-Frame Zoomable Timeline:**
  - Interactive time ruler with scalable units (frames, seconds, SMPTE timecode).
  - Multi-track layer stacking with drag-to-reorder, layer locking, visibility toggling, and track soloing.
  - Interactive playhead with sub-frame scrubbing, loop regions (In/Out markers), and playback speeds (0.25x to 4x).
- **Bezier Curve Keyframe Interpolation:**
  - Dedicated `KeyframeCurveEditorWindow.xaml` featuring cubic Bezier spatial and temporal curves.
  - Presets: Linear, Ease-In, Ease-Out, Ease-In-Out, Bounce, Spring, and Custom Bezier handles.
  - Keyframable properties: Position (X, Y, Z), Scale (X, Y, Uniform), Rotation (Degrees), Opacity (0–100%), Fill Color, Stroke Width, and Blur Radius.
  - Intelligent keyframing: Modifying an object on the canvas automatically updates the active keyframe or base geometry without resetting position.

#### 2. Hierarchical Grouping & Nested Compositions (Precompose)
- Multi-selection workflow: `Ctrl+Click` for individual toggling, `Shift+Click` for contiguous range selection.
- **Precompose Architecture:** Group any set of layers into a nested composition sub-timeline. Double-clicking enters the nested timeline; an explicit "UP" button returns to the parent canvas. Nested groups maintain their own independent animation timelines and bounding box coordinates.

#### 3. Comprehensive Broadcast Layer Types
- **Rich Text Blocks:** Advanced typography (Google Fonts, Windows System Fonts, Devanagari/Nepali Unicode fonts), character tracking, line leading, auto-shrink/auto-fit text boundary boxes, outline strokes with customizable thickness, drop shadows with soft blur, and multi-stop linear/radial gradients.
- **Smooth Horizontal Crawl / Ticker:** Sub-pixel smooth horizontal movement with configurable speed (pixels/sec), separator icons, loop counts, and pause-on-air triggers.
- **Vertical Credit Roll:** Broadcast-standard vertical scrolling with speed damping and auto-end triggers.
- **Vector Shapes & Containers:** Rectangles, rounded boxes (with independent corner radii), circles, ellipses, polygons, lines, and dynamic containers that automatically expand or contract based on text length.
- **Alpha Video & Image Sequences:** Transparent ProRes 4444 clips, WebM VP9 with alpha, loopable PNG image sequences, and SVG vector graphics.
- **Live Clocks, Timers & Calendars:** Real-time digital clocks (12h/24h), analog station clocks, countdowns, count-ups, and dual Gregorian/Nepali calendar date tags.

#### 4. Dynamic Data-Binding & Tag Architecture
- Reusable template fields: Any layer property can be linked to dynamic tags (e.g., `{Headline}`, `{GuestName}`, `{GuestTitle}`, `{HomeScore}`, `{AwayScore}`, `{Temperature}`).
- External Live Connectors: Ingest dynamic data from REST JSON APIs, XML endpoints, RSS feeds, CSV spreadsheets, and local text files.
- Export to `.kcgt` (Kashtrix CG Template) package: Bundles fonts, vector assets, media files, and JSON layout schemas into a single portable broadcast file.

---

### Application 4: `Kashtrix.CGController` — Live On-Air Character Generator Control Surface

- **Executable:** `Kashtrix.CGController.exe`
- **Source Project:** `src/Kashtrix.CGController/Kashtrix.CGController.csproj`
- **Primary Operational Role:** Rapid, tactile on-air operator surface for triggering graphics, modifying breaking news straps, firing tickers, and managing channel bugs during live broadcasts.

#### 1. Layer Stacking & Bus Independence
- **Dual Bus Architecture (Preview & Program):**
  - **Preview Bus (PVW):** Renders the graphic template with entered data off-air. Allows operators to verify typography, text wrapping, and image loading before air.
  - **Program Bus (PGM):** Active on-air broadcast bus.
- **Independent Broadcast Layer Allocation:**
  - Graphics are assigned to dedicated numerical layers (0 to 999).
  - Non-interfering playback: Firing a lower third on Layer 3 smoothly animates without affecting an ongoing news ticker on Layer 2 or a station bug on Layer 1.

#### 2. Fast Live Data Entry Workflows
- **Instant Live Form Generation:**
  - Selecting a template automatically dynamically generates text input fields based on its exposed `{Tags}`.
  - Direct on-the-fly typing: Operators can modify breaking news headlines or guest titles while the graphic is live.
- **Live Update Execution:**
  - The `UPDATE` button pushes new text to the on-air graphic smoothly without re-triggering the entrance animation or causing visual flicker.

#### 3. Playback Controls & Timers
- **Tactile Transport Operations:**
  - `PLAY` / `TAKE IN`: Triggers the entrance animation.
  - `TAKE OUT` / `STOP`: Executes the exit animation.
  - `UPDATE`: Pushes updated text to the running layer.
  - `CLEAR LAYER`: Immediately cuts the active graphic on the selected layer.
  - `CLEAR ALL`: Emergency blackout of all graphics layers across the channel.
- **Automated Out-Timers:** Configurable display hold duration (e.g., 5s, 8s, 12s). The graphic displays, counts down, and automatically animates out without operator intervention.
- **Multi-Channel Graphics Routing:** Fire graphics simultaneously to one channel, a subset of channels, or all broadcast channels with channel-specific or synchronized data payloads.

---

### Application 5: `Kashtrix.ChannelController` — Master Channel Automation & Matrix Routing

- **Executable:** `Kashtrix.ChannelController.exe`
- **Source Project:** `src/Kashtrix.ChannelController/Kashtrix.ChannelController.csproj`
- **Primary Operational Role:** Centralized multi-channel supervision and hardware matrix router orchestration across an entire broadcast television station or network facility.

#### 1. Multi-Channel Consolidated Supervision
- Monitors multiple independent `BroadcastPlayout.App` instances running across different physical servers or virtual machines.
- Consolidated telemetry: On-air item, next cued item, elapsed/remaining time, audio peak VU levels, output arm status, HA role, and active alarm count per channel.
- Per-channel and group execution: Send synchronized commands (`GROUP TAKE`, `GROUP HOLD`, `GROUP RESUME`, `EMERGENCY SLATE`) across multiple channels simultaneously. Each channel returns individual execution receipts to ensure zero unacknowledged failures.

#### 2. Hardware Broadcast Matrix Router Integration
- Native IP and serial network control protocols:
  - **Blackmagic Videohub Protocol:** Ethernet-based crosspoint switching.
  - **Ross Video Smart Switch / Ultrix:** Native routing control.
  - **Grass Valley / Pro-Bel (SW-P-08):** Industry standard broadcast router matrix protocol.
  - **Utah Scientific & Leitch Protocols.**
- Dynamic Router Automation: Automatically switches physical router crosspoints in sync with playlist events (e.g., switching Studio 1 Live Feed to transmission when a rundown item reaches 00:00:00).

#### 3. Automated Signal Quality Watchdog & Failover
- Continuous stream analysis on all channel outputs:
  - Black frame detection (sustained threshold > 3.0s).
  - Freeze frame detection (static video threshold > 5.0s).
  - Audio silence detection (levels < -50 dBFS > 4.0s).
- Automated Emergency Actions: Automatically switches router crosspoint to emergency backup server, triggers standby station slate, or fires an automated audio/video loop.

---

### Application 6: `Kashtrix.PlaylistEditor` — Broadcast Rundown & Multi-Part Log Manager

- **Executable:** `Kashtrix.PlaylistEditor.exe`
- **Source Project:** `src/Kashtrix.PlaylistEditor/Kashtrix.PlaylistEditor.csproj`
- **Primary Operational Role:** Dedicated rundown authoring, timing verification, and transmission log preparation tool for broadcast operators and traffic managers.

#### 1. 24-Hour Rundown Construction & Mathematical Timing
- Frame-accurate calculation of event start times, durations, commercial blocks, and broadcast over/under runs across the 24-hour transmission day.
- Visual thumbnail previews, category color coding, and automatic dimming of completed items.
- Event Types:
  - `MEDIA`: Prerecorded video clip (ProRes, XDCAM, DNxHD, H.264).
  - `LIVE INPUT`: Physical SDI input, NDI feed, or SRT stream.
  - `STOP`: Deliberate manual cue point (awaits operator TAKE).
  - `PLAY`: Programmed play trigger.
  - `PAUSE`: Timed pause point.
  - `NOTE / SCRIPT`: Editorial comment and talent presenter cues.

#### 2. Multi-Part Non-Destructive Media Trimming
- Define multiple non-contiguous In/Out trim segments on a single media file.
- Example: Segment 1 (00:00 to 12:30), Commercial Break, Segment 2 (14:30 to 25:00).
- The playout engine plays all defined parts as one seamless continuous item without re-encoding or exporting new video files.

#### 3. In-Flight Live Rundown Modification
- Add, duplicate, delete, reorder, or modify rundown items while the playlist is actively playing on air.
- Drag-and-drop integration: Drop files directly from Windows Explorer, Kashtrix MAM, or Kashtrix File Manager into the active rundown grid.
- Import/Export formats: Kashtrix XML, Broadcast Exchange Format (BXF), M3U8, and standard CSV traffic logs.

---

### Application 7: `Kashtrix.Scheduler` — Long-Term Programming Grid & Commercial Traffic

- **Executable:** `Kashtrix.Scheduler.exe`
- **Source Project:** `src/Kashtrix.Scheduler/Kashtrix.Scheduler.csproj`
- **Primary Operational Role:** Long-range 24/7 calendar grid scheduling, commercial break campaign placement, competitor ad collision prevention, and As-Run compliance reconciliation.

#### 1. Calendar-Based Master Programming Grid
- Visual 24-hour horizontal day timeline (00:00 to 24:00) with interactive program blocks.
- Recurrence engine: Schedule daily, weekly, weekday, weekend, or custom CSV day-pattern series.
- Dayparting and automated program slot allocation (Morning News, Afternoon Drama, Prime-Time Movies, Late-Night Sports).

#### 2. Commercial Traffic & Ad Separation Engine
- Commercial ad pod management: Define maximum break durations and exact spot counts.
- **Advertiser Separation Rules:** Intelligent collision prevention algorithms automatically prevent placing competing advertiser spots (e.g., two rival beverage companies) within the same commercial break or adjacent blocks.
- Campaign fulfillment tracking: Monitor contracted spot flights, demographic targets, and target impression counts.

#### 3. Gap Management & Automated Filler Insertion
- Identifies programming gaps between scheduled shows.
- Automatic filler policy: When a program finishes early or a satellite feed drops, the engine automatically selects and inserts broadcast filler (promos, station IDs, music clips) from a designated filler folder.

#### 4. As-Run Audit Logs & EPG Generation
- **Cryptographic As-Run Verification:** Logs exact aired start timecode, actual duration, discrepancy offsets, and transmission channel for every aired asset.
- Export to industry billing, invoicing, and accounting software (CSV, XML, PDF).
- **Electronic Program Guide (EPG) Generator:** Generates compliant DVB EIT (Event Information Table) data and XMLTV schedules for cable/satellite operators and web portals.

---

### Application 8: `Kashtrix.MAM` — Enterprise Media Asset Management & Catalog System

- **Executable:** `Kashtrix.MAM.exe`
- **Source Project:** `src/Kashtrix.MAM/Kashtrix.MAM.csproj`
- **Primary Operational Role:** Centralized broadcast asset repository cataloging all video, audio, graphic, and promotional media across the broadcast enterprise.

#### 1. Asset Cataloging & Metadata Schema
- Assigns permanent enterprise Asset GUIDs and tracks version history.
- Metadata schema: Title, Episode Number, Season, House ID, Tape ID, Category (Media, News, Commercial, Music, Graphics, Archive), Tags, Description, Rights Holder, and Licensor Notes.
- Technical parameters: Video format, raster size, aspect ratio, frame rate, audio channel layout, subtitle tracks, and duration.

#### 2. Background Proxy & Contact-Sheet Generation
- Autonomous background worker converts multi-gigabyte broadcast masters into lightweight H.264 proxy files (`.mp4`) for instant operator scrubbing over local networks or VPNs.
- Automatic generation of multi-tile visual contact sheets and frame-accurate scrubber thumbnails.

#### 3. Multi-Tier Hierarchical Storage Management (HSM)
- Seamless asset movement across storage tiers:
  - *Tier 1 (Online):* High-speed NVMe/SSD local storage for active playout servers.
  - *Tier 2 (Nearline):* Central 10GbE/40GbE SAN/NAS storage for newsroom and editor access.
  - *Tier 3 (Deep Archive):* Automated LTO tape library, optical jukebox, or S3 cloud archive.
- Archive, Restore, and Purge jobs operate asynchronously with checksum verification.

#### 4. Cryptographic Integrity & Rights Management
- Automated calculation of SHA-256 and MD5 checksums upon asset ingestion; periodic background scrubbing detects bitrot or file corruption.
- Workflow & Approval states: `Draft`, `Review`, `Approved`, `Rejected`, `Blocked`. Playout engines refuse to load unapproved assets.
- Rights windows: Enforces Rights Start Date and Rights Expiry Date; expired assets are automatically barred from transmission.

---

### Application 9: `Kashtrix.FileManager` — Media Explorer & Fast Ingest

- **Executable:** `Kashtrix.FileManager.exe`
- **Source Project:** `src/Kashtrix.FileManager/Kashtrix.FileManager.csproj`
- **Primary Operational Role:** High-speed media browser, hot-folder watcher, and automated file normalization tool designed for broadcast server environments.

#### 1. Smart Hot-Folder Ingest Watchers
- Continuously monitors local folders, network shares, FTP/SFTP dropboxes, and removable USB camera drives for incoming video files.
- Stabilized file detection: Waits for external file transfers to complete (checks file handles and size stability) before triggering ingest workflows.

#### 2. Automated Format Normalization & Transcoding
- Automatically converts consumer formats (cellphone recordings, Zoom clips, variable frame rate MP4s, MKV) into standardized station house broadcast formats:
  - Apple ProRes 422 (1080i50 / 1080p50).
  - Sony XDCAM HD422 (50 Mbps CBR MXF OP1a).
  - Avid DNxHD 120/185 Mbps.
- Conforms non-standard frame rates (e.g., 29.97 fps, 24.0 fps) to the master station frame rate (50.00 fps or 59.94 fps) using motion interpolation.

#### 3. Checksum Verification on Transfer
- Bit-exact file transfers between ingest workstations and playout storage arrays with integrated SHA-256 validation.

---

### Application 10: `Kashtrix.IngestServer` — Baseband & Multi-Channel IP Ingest Recorder

- **Executable:** `Kashtrix.IngestServer.exe`
- **Source Project:** `src/Kashtrix.IngestServer/Kashtrix.IngestServer.csproj`
- **Primary Operational Role:** Dedicated multi-stream baseband SDI and IP recording engine for live sports, news studio feeds, satellite downlinks, and 24/7 compliance logging.

#### 1. Multi-Channel Simultaneous Capture
- Concurrent capture across multiple independent inputs:
  - Blackmagic DeckLink SDI/HDMI cards.
  - AJA Kona & Magewell Pro Capture cards.
  - Native NDI 6 network streams (auto-scans network every 10 seconds).
  - IP streams: SRT (Caller/Listener), RTMP, RTSP, and UDP Transport Streams.
- Per-channel live confidence monitor with real-time video preview and peak audio level meters.

#### 2. Edit-While-Ingest (Growing File Support)
- Records directly into broadcast MXF OP1a or QuickTime MOV formats with continuously updated file headers and index tables.
- Playout servers (`BroadcastPlayout.App`) and NLE editing suites (Adobe Premiere, Grass Valley EDIUS) can open and broadcast the recording while the event is still actively being captured.

#### 3. Compliance Logging & Segmentation
- Continuous 24/7 legal compliance recording.
- Automated segment splitting: Chunks long recordings into user-definable segments (e.g., 15, 30, or 60 minutes) with zero lost frames between files.
- Dual Master + Proxy recording: Simultaneously captures a high-bitrate broadcast master (ProRes / XDCAM) and a lightweight H.264 web review proxy.

#### 4. Recording Trigger Modes
- **Crash Record:** Instant manual one-click recording.
- **Scheduled Record:** Pre-programmed start and stop date/time linked to master calendar.
- **VTR Ingest:** RS-422 Sony 9-Pin deck control for automated tape batch capture.
- Automated MAM registration upon capture completion.

---

### Application 11: `Kashtrix.InputEngine` — Live Video Hub & Frame Synchronizer

- **Executable:** `Kashtrix.InputEngine.exe`
- **Source Project:** `src/Kashtrix.InputEngine/Kashtrix.InputEngine.csproj`
- **Primary Operational Role:** Hardware signal acquisition engine, IP receiver matrix, and broadcast frame synchronizer converting live physical inputs and network streams into internal feeds.

#### 1. Signal Acquisition Matrix
- Baseband Cards: Blackmagic DeckLink, AJA Kona, Magewell, and DirectShow capture devices with user-selectable rasters and frame rates.
- IP Stream Receiver: SRT (Caller/Listener with AES decryption), NDI 6, RTSP, RTMP, and UDP Multicast.
- Direct Web Video Resolution: Integrated YouTube and web URL stream resolver (via bundled `yt-dlp`).
- Windows Desktop Capture: Low-latency DXGI desktop screen capture with optional mouse cursor overlay.

#### 2. Broadcast Frame Synchronizer & De-Interlacer
- Synchronizes unsynchronized remote feeds to the master station genlock clock.
- Dynamic jitter buffer eliminates network packet jitter on incoming remote SRT contribution feeds.
- Studio-grade motion-adaptive de-interlacing (576i, 1080i to progressive frames).
- Color space conversion: Rec.601, Rec.709, and Rec.2020 color matrices with full RGB/YUV range scaling.
- Audio pitch-safe re-sampling: Re-samples remote audio clocks to exact 48.000 kHz station clock without audible pitch distortion.

#### 3. Kashtrix Studio (K-Mix) Shared Memory Bus
- Direct zero-copy inter-process shared memory connection to Kashtrix Studio:
  - Video Bus: `KashtrixStudio.Program.Video.v1`
  - Audio Bus: `KashtrixStudio.Program.Audio.v1`
- Enables uncompressed 1080p60/50 video and 8-channel audio transfer between studio production switchers and master playout with zero CPU encoding overhead and < 1ms latency.

---

### Application 12: `Kashtrix.QCController` — Automated Broadcast Quality Control

- **Executable:** `Kashtrix.QCController.exe`
- **Source Project:** `src/Kashtrix.QCController/Kashtrix.QCController.csproj`
- **Primary Operational Role:** Automated broadcast compliance verification engine validating audio loudness, video integrity, and technical container syntax prior to on-air playout.

#### 1. Dual QC Modes (Fast QC vs. Deep QC)
- **Fast QC (Container & Header Inspection):** Rapid metadata probe verifying file duration, codec conformance, raster dimensions, frame rate, audio track count, sample rate, and bit depth. Validates files in seconds.
- **Deep QC (Frame-by-Frame Video & Audio Decode):** Fully decodes video and audio streams from start to end using hardware/software decoders to detect deep bitstream corruption, damaged GOPs, and decoding syntax errors.

#### 2. Audio Loudness Compliance (EBU R128 & ITU-R BS.1770-4)
- Measures Integrated Loudness (LUFS / LKFS) against target broadcast standards (e.g., -23.0 LUFS ±0.5 LU for EBU; -24.0 LUFS for ATSC A/85).
- Calculates Loudness Range (LRA), Maximum Short-Term Loudness, Momentary Loudness, and Maximum True Peak (dBTP).
- Flags loudness violations and generates non-destructive gain correction metadata.

#### 3. Video Anomaly & Artifact Detection
- **Black Frame Guard:** Detects continuous black video exceeding permissible thresholds (e.g., > 2.0s).
- **Freeze Frame Guard:** Detects static/frozen video frames indicative of upstream satellite or encoder freezes.
- **Aspect Ratio Violations:** Detects unintended letterboxing, pillarboxing, or anamorphic squeeze distortion.
- **Macroblock & Digital Dropouts:** Identifies severe blockiness, pixelation, and dropped frames.

#### 4. QC Certification & Playout Interlocking
- Generates detailed Mediatime-style technical reports (stream profiles, chroma format 4:2:2/4:2:0, bit depth 8/10-bit, audio channels).
- Automatically stamps MAM database records: Non-compliant media is marked `FAILED` and prevented from loading into live playout rundowns.

---

### Application 13: `Kashtrix.Multiview` — Broadcast Multi-Tile Mosaic Wall

- **Executable:** `Kashtrix.Multiview.exe`
- **Source Project:** `src/Kashtrix.Multiview/Kashtrix.Multiview.csproj`
- **Primary Operational Role:** Studio mosaic monitoring wall displaying multiple channel outputs, live satellite feeds, server preview channels, and alarm heads-up displays on single or multi-screen video walls.

#### 1. Flexible Multi-Tile Mosaic Layouts
- Layout Presets: 1+4 (Master PGM + 4 Inputs), 2x2 (4 Channels), 3x3 (9 Tiles), 4x4 (16 Tiles), 8+2, and custom non-uniform aspect ratio grids.
- Multi-monitor support: Span across up to four 4K UHD video wall display outputs.

#### 2. Heads-Up Display (HUD) Overlays
- **Broadcast Tally Borders:**
  - Red Border: Program Bus (Live On-Air).
  - Green Border: Preview Bus (Cued / Next to Air).
  - Amber Border: Standby / Armed.
- **Audio PPM & VU Peak Meters:** Real-time multi-channel digital audio level bars with peak-hold indicators and dBFS numerical readouts.
- **Running Metadata Badges:** Channel ID, clip title, video raster/framerate (e.g., `1080i50 · ProRes · Stereo`), SMPTE timecode clock, and clip countdown timer.
- **Visual Alert Badges:**
  - `BLK` (Red): Active black frame detected.
  - `FRZ` (Amber): Video freeze detected.
  - `SIL` (Red): Audio silence detected.
  - `35` (Purple): SCTE-35 splice packet detected.

#### 3. Interactive Tile Confidence Monitoring
- Double-clicking any mosaic tile opens `MultiviewTileFullscreenWindow.xaml`, instantly expanding the feed to full-screen confidence monitoring with active audio listening on control room speakers.

---

### Application 14: `Kashtrix.NRCS` — Newsroom Computer System (MOS 2.8.4) & Dual Calendar Desk

- **Executable:** `Kashtrix.NRCS.exe`
- **Source Project:** `src/Kashtrix.NRCS/Kashtrix.NRCS.csproj`
- **Primary Operational Role:** Full-featured television newsroom system managing story assignments, wire services, journalist scriptwriting, MOS 2.8.4 rundown synchronization, and localized calendar planning.

#### 1. Comprehensive Newsroom Workspace
- Dedicated Multi-Tab Architecture:
  - `DASHBOARD`: Active rundowns, ready stories, open assignments, wire feeds, and transmission KPIs.
  - `PLANNING / ASSIGNMENTS`: Long-term editorial assignment planning, camera crew dispatch, and reporter deadlines.
  - `ASSIGNMENTS`: Detailed assignment board with progress tracking (`Pitch`, `Assigned`, `Filming`, `Editing`, `Approved`).
  - `WIRES / INGEST`: Ingests Reuters, AP, AFP, and custom RSS wire services; one-click wire-to-story conversion.
  - `STORY DESK`: Full-featured journalist scriptwriting editor with teleprompter body, presenter cues, technical directions, and estimated reading duration calculations.
  - `RUNDOWN / LIVE`: Master news rundown grid with real-time story order, timing mathematics, and live on-air story tracking.
  - `PROGRAMS & PRESETS`: Recurring news bulletin templates (Morning News, 7 PM Bulletin, Special Coverage).
  - `CATEGORIES`: News categorization (Politics, Economy, Sports, World, Tech, Culture).
  - `INTEGRATION / STATUS`: Live MOS socket status, playout engine connections, and audit trails.

#### 2. MOS Protocol 2.8.4 Integration
- Bi-directional Media Object Server (MOS) protocol implementation over TCP/IP sockets (ports 10540/10541):
  - Integrates with Avid iNEWS, AP ENPS, Octopus Newsroom, Dalet News, and Ross Inception.
  - Real-time rundown synchronization: When news producers reorder stories or break into programming, changes are instantly reflected on the master playout rundown.
  - MOS Object Linking: Attach MAM media clips, lower-third CG templates (with journalist-entered text fields), and commercial breaks directly into story slugs.

#### 3. Dual Gregorian & Nepali Calendar Engine
- Dedicated `NepaliCalendarDialog.xaml`:
  - Bidirectional date conversion between Gregorian (AD) and Bikram Sambat (BS) calendar systems.
  - Supports Nepali calendar event scheduling, national holiday indicators, and Nepali Devanagari date string formatting for broadcast CG tickers and newsroom rundowns.

---

### Application 15: `Kashtrix.Prompter` — Studio Teleprompter & Anchor Display Engine

- **Executable:** `Kashtrix.Prompter.exe`
- **Source Project:** `src/Kashtrix.Prompter/Kashtrix.Prompter.csproj`
- **Primary Operational Role:** Broadcast studio teleprompter engine delivering smooth, high-precision script scrolling for news anchors, presenters, and studio talent.

#### 1. Hardware-Accelerated Smooth Scrolling
- DirectWrite and GPU-accelerated typography rendering eliminating micro-stutters, tearing, and line jumping at all scroll speeds.
- Continuously variable scrolling speed with smooth acceleration and deceleration curves.

#### 2. Multi-Output & Optical Mirroring
- **Optical Inversion Modes:** Instant Horizontal Flip (mirror) and Vertical Inversion (flip) for through-the-lens studio glass beam-splitter rigs.
- **Direct NDI Output:** Transmits full prompter video over the network via NDI with configurable resolution (1080p, 720p), framerate, and safe action/title margins.
- **Direct SDI / DeckLink Output:** Broadcasts prompter video directly through Blackmagic DeckLink SDI cards to studio camera monitors.
- **Local Secondary Display:** Pops out borderless fullscreen prompter window (`PrompterDisplayWindow.cs`) on auxiliary graphics card display heads.

#### 3. Hardware Controller & Remote Protocol
- Dedicated UDP Remote Control Server (default port `7788`):
  - Integrates with wireless USB foot pedals, handheld thumb scroll controllers, and Contour Shuttle Pro hardware.
  - Remote commands: Scroll, Pause, Speed Up, Speed Down, Jump Next Story, Jump Previous Story, Top of Rundown.

#### 4. Real-Time NRCS Context & Presenter Eyeline Guide
- WinPlus-style live newsroom context: Displays active story slug, journalist notes, talent cue markers, and on-air playout channel status alongside the scrolling script.
- Prompter script updates in real time as journalists edit copy in Kashtrix NRCS or via MOS without interrupting the anchor's active reading position.
- Adjustable on-screen talent eyeline marker with customizable color, width, and position.

---

### Application 16: `Kashtrix.HAController` — High Availability & Redundancy Controller

- **Executable:** `Kashtrix.HAController.exe`
- **Source Project:** `src/Kashtrix.HAController/Kashtrix.HAController.csproj`
- **Primary Operational Role:** Enterprise failover and redundancy supervisor ensuring zero-downtime, continuous transmission across playout server clusters.

#### 1. Redundancy Topologies
- **1+1 Hot Standby (Main / Backup):** Primary and secondary playout engines decode the identical rundown in frame-lock synchrony.
- **N+M Shared Backup Coordinator:** Multi-channel clusters where dedicated standby servers dynamically take over for any failed primary channel in the pool.

#### 2. High-Frequency Heartbeat & State Synchronization
- Redundant network heartbeat telemetry evaluating:
  - Process responsiveness and OS thread health.
  - Direct3D 11 render loop frame presentation latency.
  - SDI hardware DMA lock and genlock reference status.
  - Network I/O throughput and storage mount availability.
- Mirroring Engine: Continuously synchronizes playlist items, cued item pointers, active elapsed frame offsets, and graphics layer states to the backup node.

#### 3. Output Interlock Safety System
- Strict mechanical and logical transmission interlocking:
  - **Active Node:** Transmission output is `ARMED`.
  - **Standby Node:** Decodes video in full synchrony for instant readiness, but physical transmission output is strictly `INHIBITED` to prevent dual-feed transmission collisions.

#### 4. Seamless Automated Failover & Disaster Drills
- Sub-frame failure detection: If the primary node experiences hardware failure, power loss, process termination, or frozen video for > 3.0 seconds, the HA Controller immediately executes failover:
  - Switches SDI matrix router crosspoint to backup node.
  - Arms the backup server's transmission output in < 1 frame (40ms).
- Operator Controls:
  - `FORCE SELECTED ACTIVE`: Manual master takeover.
  - `SIMULATE FAILOVER`: Triggers disaster recovery drill to validate automated standby takeover.
  - `RESTORE / FAILBACK`: Gracefully re-engages recovered primary node without taking channel off air.
  - `RELEASE TO AUTO`: Returns channel to automated N+M cluster arbitration.

---

## 4. Supporting Subsystems & Infrastructure Assemblies

---

### Subsystem 1: `Kashtrix.ApiGateway` — Central Automation & Unified Control Bus

- **Executable:** `Kashtrix.ApiGateway.exe`
- **Source Project:** `src/Kashtrix.ApiGateway/Kashtrix.ApiGateway.csproj`
- **Host Engine:** `GatewayHost.cs` (105 KB enterprise multi-protocol daemon)
- **Primary Operational Role:** Unified communication gateway exposing RESTful HTTP APIs, WebSockets, TCP command sockets, UDP datagram listeners, Model Context Protocol (MCP) servers, and an embedded Web Playout MCR.

#### 1. Network Interfaces & Port Mapping
- **RESTful HTTP API (`http://127.0.0.1:9080/api/v1/`):**
  - Standard JSON endpoints with CORS support (`*`) and Bearer/Header authentication (`X-Kashtrix-Api-Key`).
  - `/api/v1/health`: System health and active channel count.
  - `/api/v1/playout/channels`: Real-time status of all playout engines.
  - `/api/v1/playout/playlist`: Active rundown items, durations, and statuses.
  - `/api/v1/playout/commands`: Dispatches `PLAY`, `PAUSE`, `STOP`, `NEXT`, `SEEK`, `TAKE`.
  - `/api/v1/playout/asrun`: Queries As-Run compliance audit records.
  - `/api/v1/cg/templates`: Catalogs all available CG templates and graphic layers.
  - `/api/v1/cg/thumbnail`: Renders on-the-fly PNG thumbnails of graphic templates.
  - `/api/v1/cg-controller/onair`: Queries all active on-air graphic layers across channels.
  - `/api/v1/cg-controller/play`, `update`, `stop`, `clear`: Live graphics triggering.
  - `/api/v1/mam/assets`: Queries enterprise MAM library and asset metadata.
  - `/api/v1/nrcs/rundowns`, `stories`, `assignments`: Full newsroom data interchange.
  - `/api/v1/ha/nodes`, `failover`, `release`: Cluster redundancy status and manual takeover.
  - `/api/v1/media/serve`: HTTP Range-request capable media server for browser video preview.
- **Real-Time WebSocket Bus (`ws://127.0.0.1:9080/api/v1/events/ws`):**
  - Bidirectional 1-second interval event streaming broadcast (`platform.status`).
  - Accepts immediate JSON automation commands from web control clients.
- **Machine-to-Machine TCP JSONL Automation Socket (Port `9090`):**
  - Persistent line-delimited JSON socket for high-speed master control automation systems.
  - Handshake authentication, channel subscription, and sub-millisecond command execution.
- **Low-Latency UDP Datagram Listener (Port `9091`):**
  - One-way UDP listener for legacy broadcast controllers, physical vision mixer GPI buttons, and automation triggers with optional ACK responses.
- **Model Context Protocol (MCP) Server (`POST /mcp`):**
  - Standard JSON-RPC 2.0 endpoint for AI agent orchestration and workflow automation:
    - `playout_get_status`, `playout_command`
    - `channel_controller_list`, `channel_controller_command`
    - `cg_play`, `cg_stop`, `cg_get_onair`
    - `nrcs_list_rundowns`, `nrcs_list_stories`, `nrcs_get_live`, `nrcs_set_live`, `nrcs_publish_playout`
    - `ha_get_state`, `ha_force_active`, `ha_release`
    - `demo_load_and_test`

#### 2. Embedded Web Playout Client (Web MCR)
- Lightweight HTML5/JavaScript dashboard served directly at `http://127.0.0.1:9080/`.
- Enables remote channel monitoring, rundown inspection, and emergency commands from any tablet, mobile phone, or desktop browser.

---

### Subsystem 2: `Kashtrix.Settings` — Central Configuration & Hardware Hub

- **Executable:** `Kashtrix.Settings.exe`
- **Source Project:** `src/Kashtrix.Settings/Kashtrix.Settings.csproj`
- **Primary Operational Role:** Centralized configuration management utility storing hardware mappings, video rasters, GPU preferences, and security access policies.

#### 1. Video Standards & Hardware Profiles
- Master Raster Configuration: 1080i50, 1080p50, 1080p59.94, 1080p60, 720p50, 720p59.94, 2160p50, 2160p59.94.
- Hardware Acceleration: NVDEC/NVENC adapter index, Intel QSV activation, and DXVA2 fallbacks.
- Professional Baseband Cards: DeckLink and AJA port routing, input/output device indexing, genlock timing offsets.

#### 2. Audio Routing & Master Volume Processing
- Program Audio Device vs. Preview/Audition Audio Device mapping.
- Master output volume trim (0–200%), master mute shortcuts, and audio delay calibration.

#### 3. Output Video Processor
- Dedicated post-compositor video processing engine: Real-time Brightness, Contrast, Saturation, Gamma, Hue, Exposure, Color Temperature, Tint, Levels, Sharpness, and discrete RGB channel gains.

#### 4. Security Store & Role-Based Access Control (RBAC)
- Multi-user authentication: Manages operator accounts, hashed passwords, and granular API scopes:
  - `playout:read`, `playout:write`
  - `cg:read`, `cg:write`
  - `mam:read`, `mam:write`
  - `nrcs:read`, `nrcs:write`
  - `ha:read`, `ha:write`
  - `prompter:read`, `prompter:write`
  - `mcp:access`

---

### Subsystem 3: `Kashtrix.Shared` — Core Engine & Format Library

- **Assembly:** `Kashtrix.Shared.dll`
- **Source Project:** `src/Kashtrix.Shared/Kashtrix.Shared.csproj`
- **Primary Operational Role:** Common core libraries, mathematical calculation engines, binary metadata parsers, and unified WPF broadcast visual styles.

#### 1. Core Modules & Algorithms
- **SMPTE 12M Timecode Engine:** High-precision drop-frame and non-drop-frame timecode calculations (frames, seconds, minutes, hours) supporting 23.976, 24, 25, 29.97, 30, 50, 59.94, and 60 fps.
- **SCTE-35 Binary Serializer:** Bitstream encoding and decoding of SCTE-35 splice commands, descriptor syntax, and CRC32 verification.
- **DVB SI/PSI Descriptor Generators:** Generates raw MPEG-2 TS section filters and binary descriptors for PAT, PMT, SDT, NIT, and TDT tables.
- **Kashtrix Broadcast Theme:** Custom dark-mode WPF styles, custom window chrome handlers, digital clock font loaders (`Digital-7`, `DSEG7`, `Consolas`), vector broadcast icons, and color palettes.

---

## 5. Protocols, Ports & Inter-Process Communication (IPC) Reference

| Protocol / Bus | Port / Path / Resource | Direction | Handling Component | Purpose & Operational Use |
|---|---|---|---|---|
| **REST HTTP API** | `TCP 9080` (`/api/v1/*`) | Inbound | `Kashtrix.ApiGateway` | External automation, MAM queries, rundown commands, Web MCR |
| **Realtime WebSocket** | `TCP 9080` (`/api/v1/events/ws`) | Inbound/Out | `Kashtrix.ApiGateway` | Live broadcast status streaming (1s tick) and reactive commands |
| **MCP AI Server** | `TCP 9080` (`/mcp`) | Inbound | `Kashtrix.ApiGateway` | Model Context Protocol JSON-RPC for AI agents and orchestrators |
| **TCP Command Socket** | `TCP 9090` | Inbound/Out | `Kashtrix.ApiGateway` | High-speed machine-to-machine line-delimited JSON automation |
| **UDP Trigger Listener** | `UDP 9091` | Inbound | `Kashtrix.ApiGateway` | Low-latency GPI and legacy broadcast controller action triggers |
| **Prompter Remote UDP** | `UDP 7788` | Inbound | `Kashtrix.Prompter` | Studio wireless foot pedals, hand controllers, Contour Shuttle |
| **MOS Protocol Upper** | `TCP 10540` | Inbound/Out | `Kashtrix.NRCS` | MOS 2.8.4 bi-directional rundown and story synchronization |
| **MOS Protocol Lower** | `TCP 10541` | Inbound/Out | `Kashtrix.NRCS` | MOS 2.8.4 heartbeats, object messages, and status exchange |
| **DVB-TS UDP Output** | `UDP 1234` (Configurable) | Outbound | `Kashtrix.OutputEngine` | Digital broadcast transport stream multicast/unicast to modulators |
| **SRT Contribution** | User-defined (e.g. `9000`) | In/Out | `OutputEngine` / `InputEngine` | Secure Reliable Transport encrypted contribution and transmission |
| **NDI Video Streams** | Dynamic (`mDNS 5353`, TCP) | In/Out | `OutputEngine`, `Input`, `CG`, `Prompter` | Uncompressed studio network video with alpha and Tally |
| **Studio Video IPC** | `KashtrixStudio.Program.Video.v1` | Shared Memory | `Kashtrix.InputEngine` | Zero-copy uncompressed video bus from Kashtrix Studio (K-Mix) |
| **Studio Audio IPC** | `KashtrixStudio.Program.Audio.v1` | Shared Memory | `Kashtrix.InputEngine` | Zero-copy 8-channel audio bus from Kashtrix Studio (K-Mix) |
| **Platform Control Bus** | Local In-Memory / File Pipe | Local IPC | All Applications | Channel status distribution (`PlatformControlBus.cs`) |
| **Local CG Command Bus** | Local Memory Queue | Local IPC | `CGController` -> `Playout` | Instant on-air graphics execution (`LocalCgCommandBus.cs`) |
| **HA Control Bus** | Local IPC / Heartbeat UDP | Local/Net | `Kashtrix.HAController` | Redundancy state, heartbeats, and failover arbitration |

---

## 6. Comprehensive Broadcast Standards & Compliance Matrix

### 6.1 Baseband & IP Video Standards
- **SMPTE 259M:** Standard Definition SDI (SD-SDI 270 Mbps / 576i50).
- **SMPTE 292M:** High Definition SDI (HD-SDI 1.485 Gbps / 1080i50, 720p50).
- **SMPTE 424M:** 3G-SDI (2.970 Gbps / 1080p50, 1080p59.94 Level A & B).
- **SMPTE ST 2082:** 12G-SDI single-link 4K Ultra HD (2160p50/59.94/60).
- **SMPTE ST 2110-20/30/40:** Professional media over managed IP networks (Uncompressed video, audio, ANC data).
- **SMPTE ST 2022-6 / ST 2022-7:** High-bitrate IP transport with hitless seamless packet redundancy.
- **IEEE 1588 PTP:** Precision Time Protocol for sub-microsecond studio network synchronization.

### 6.2 Audio Standards
- **EBU R128:** Loudness recommendation for European broadcast television (-23.0 LUFS target).
- **ITU-R BS.1770-4:** Algorithms to measure audio program loudness and true-peak audio levels.
- **ATSC A/85:** Techniques for establishing and maintaining audio loudness for digital television (-24.0 LUFS target).
- **AES3 / EBU Audio:** Embedded 24-bit 48.000 kHz uncompressed linear PCM broadcast audio.

### 6.3 Ancillary Data & Metadata Standards
- **SMPTE 2010 / SCTE-35:** Digital Program Insertion Cueing Message for Cable, Terrestrial, and Satellite TV.
- **SMPTE 12M-1 / 12M-2:** Linear Timecode (LTC) and Vertical Interval Timecode (VITC).
- **CEA-608 / CEA-708:** Broadcast line 21 and digital closed captions embedded in VANC.
- **OP-47 / DVB Subtitles:** Teletext and bitmap subtitles for European and Asian digital transmissions.
- **ETSI EN 300 468:** Specification for Service Information (SI) in DVB systems.
- **MOS Protocol 2.8.4:** Media Object Server Communications Protocol for newsroom computer systems.

---

## 7. Master Application Summary Table

| # | Application | Executable Name | Primary Broadcast Role | Core Protocols & Technologies | Key UI Surfaces & Dialogs |
|---|---|---|---|---|---|
| **1** | **Master Playout** | `BroadcastPlayout.App.exe` | Master Playout & DSK Compositor | Direct3D11, FFmpeg 7, NVDEC, DSK, SMPTE 12M | `MainWindow`, `AudioMixer`, `MediaTrimmer`, `PlaylistItemProperties` |
| **2** | **Output Engine** | `Kashtrix.OutputEngine.exe` | SDI Baseband & IP Transmission | DeckLink, NDI 6, DVB-TS, SCTE-35, SRT, RTMP, NVENC | `MainWindow`, `AddOutputDialog`, `InspectOutputDialog` |
| **3** | **CG Editor** | `Kashtrix.CGEditor.exe` | Motion Graphic Template Designer | Direct2D, Bezier Curves, Precompose, Dynamic Tags | `MainWindow`, `KeyframeCurveEditor`, `ColorPicker` |
| **4** | **CG Controller** | `Kashtrix.CGController.exe` | Live On-Air Graphics Surface | Multi-Layer Stacking, Live Text Input, Auto-Timers | `CgControllerWindow`, `DynamicFieldsDialog` |
| **5** | **Channel Controller** | `Kashtrix.ChannelController.exe` | Multi-Channel & Matrix Switcher | Videohub, Ross, Grass Valley, Signal Watchdogs | `ChannelControllerWindow`, `ChannelManagerWindow` |
| **6** | **Playlist Editor** | `Kashtrix.PlaylistEditor.exe` | 24/7 Rundown & Multi-Part Trim | Non-Destructive Multi-Part, BXF, CSV, Timing Math | `PlaylistEditorWindow`, `PlaylistItemPropertiesWindow` |
| **7** | **Scheduler** | `Kashtrix.Scheduler.exe` | Program Grid & Ad Traffic | Dayparting, Ad Collision Rules, EPG, As-Run Audit | `SchedulerWindow`, `EpgOutputSettingsWindow` |
| **8** | **MAM** | `Kashtrix.MAM.exe` | Media Asset Management & HSM | SHA-256 Hashes, Proxy Worker, Tier 1/2/3 Storage | `MediaAssetManagementWindow`, `KashtrixFileDialog` |
| **9** | **File Manager** | `Kashtrix.FileManager.exe` | Hot-Folder Explorer & Ingest | Folder Watchers, Auto-Transcode, Checksum Hash | `FileManagerWindow` |
| **10** | **Ingest Server** | `Kashtrix.IngestServer.exe` | Multi-Stream Recording & VTR | Edit-While-Ingest (Growing Files), SDI/NDI/SRT | `IngestServerWindow` |
| **11** | **Input Engine** | `Kashtrix.InputEngine.exe` | Live Video Hub & Frame Sync | Frame Synchronizer, DeckLink, NDI, K-Mix Shared Mem | `InputSourceWindow` |
| **12** | **QC Controller** | `Kashtrix.QCController.exe` | Automated Broadcast QC | EBU R128, Freeze/Black Guard, Deep Decode | `QcControllerWindow` |
| **13** | **Multiview** | `Kashtrix.Multiview.exe` | Studio Monitoring Mosaic Wall | Direct3D Mosaic, PPM/VU Meters, Tally, Signal Alerts | `MultiviewWindow`, `MultiviewTileFullscreenWindow` |
| **14** | **NRCS** | `Kashtrix.NRCS.exe` | Newsroom Computer System | MOS Protocol 2.8.4, iNEWS / ENPS Sync, Nepali Cal | `MainWindow`, `NepaliCalendarDialog`, `AddBreakDialog` |
| **15** | **Prompter** | `Kashtrix.Prompter.exe` | Studio Teleprompter Engine | Sub-Pixel DirectWrite, Optical Mirror/Flip, UDP Remote | `MainWindow`, `PrompterDisplayWindow` |
| **16** | **HA Controller** | `Kashtrix.HAController.exe` | Redundancy & Failover Manager | Heartbeat Watchdog, State Mirroring, Output Interlock | `MainWindow`, `AddHaPlayoutWindow` |
| **S1**| **API Gateway** | `Kashtrix.ApiGateway.exe` | Unified Remote Automation Bus | REST API, WebSockets, TCP 9090, UDP 9091, MCP /mcp | `MainWindow`, Web MCR (`http://localhost:9080`) |
| **S2**| **Settings** | `Kashtrix.Settings.exe` | Central System Configuration | Video Standards, GPU Config, RBAC, Video Processor | `SettingsWindow`, `VideoProcessorWindow` |
| **S3**| **Shared Core** | `Kashtrix.Shared.dll` | Engine Assembly & Algorithms | SMPTE 12M, SCTE-35, DVB SI/PSI, Custom Theme | Unified styling and shared binary engines |

---

## 8. Operational Acceptance & Pre-Air Verification Protocol

Before placing the Kashtrix Broadcast Playout Suite into live 24/7 on-air transmission, engineering staff must complete the following standard operational acceptance procedure:

1. **Clean Production Build Verification:**
   - Execute `_build.cmd` or Visual Studio Release x64 compilation. Verify that all 19 projects compile with zero errors.
2. **Channel Playout & Hardware Genlock Check:**
   - Launch `BroadcastPlayout.App.exe`. Verify Program and Preview rendering at targeted station raster (e.g., 1080i50).
   - Launch `Kashtrix.OutputEngine.exe`. Provision DeckLink SDI output; verify reference genlock lock LED and stable 50.00/59.94 Hz clocking on a waveform monitor.
3. **Graphics Preview & Take Validation:**
   - Launch `Kashtrix.CGController.exe`. Load master station package; verify that firing graphics to `PREVIEW` does not disturb `PROGRAM`.
   - Execute `TAKE` and `UPDATE` on live text fields; verify that on-air lower thirds update seamlessly without animation re-triggering.
4. **MAM & Quality Control Verification:**
   - Register master media in `Kashtrix.MAM.exe`. Confirm proxy file creation and SHA-256 hash generation.
   - Run Fast and Deep QC in `Kashtrix.QCController.exe`. Confirm that audio loudness matches target EBU R128 (-23 LUFS) and non-compliant test media is flagged.
5. **Live Ingest & Edit-While-Ingest Verification:**
   - Launch `Kashtrix.IngestServer.exe`. Start recording an incoming SDI/NDI feed into MXF OP1a.
   - While actively recording, load the growing file into `BroadcastPlayout.App.exe` and begin playback; confirm zero playback stutter.
6. **Newsroom & Teleprompter Sync:**
   - Create a rundown in `Kashtrix.NRCS.exe`. Add stories with teleprompter copy and attached CG lower-thirds.
   - Click `PROMPTER` to launch `Kashtrix.Prompter.exe`. Verify that story text appears in run order and scroll speed responds to UDP controllers.
7. **High Availability Disaster Drill:**
   - Launch `Kashtrix.HAController.exe` with primary and backup nodes online.
   - Terminate the primary playout process or disconnect primary network cable; verify that backup node switches transmission output within ≤ 1 frame (40ms) with zero visible black frames.
8. **As-Run Audit Trail Reconciliation:**
   - Play a scheduled commercial break block. Verify that `Kashtrix.Scheduler.exe` and `BroadcastPlayout.App.exe` generate matching As-Run log records with frame-accurate start timecodes and durations.
