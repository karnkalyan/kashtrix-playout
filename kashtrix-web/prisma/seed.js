const { PrismaClient } = require('@prisma/client');

const prisma = new PrismaClient();

async function main() {
  console.log('Seeding Kashtrix Broadcast Suite database...');

  // Clear existing
  await prisma.productScreenshot.deleteMany();
  await prisma.productFeature.deleteMany();
  await prisma.product.deleteMany();
  await prisma.heroContent.deleteMany();
  await prisma.siteSetting.deleteMany();
  await prisma.testimonial.deleteMany();

  // 1. Hero Content
  await prisma.heroContent.create({
    data: {
      badge: 'NEXT-GENERATION BROADCAST ARCHITECTURE',
      title: 'Enterprise TV Playout & Live Production',
      gradientTitle: 'Unified in Kashtrix',
      subtitle:
        'A complete broadcast ecosystem designed for 24/7 mission-critical television channels, newsrooms, sports production, and streaming networks. Built with baseband SDI, ST 2110, NDI 6, real-time 3D motion graphics, MAM, and automated failover.',
      primaryCta: 'Explore Broadcast Suite',
      primaryCtaUrl: '/products',
      secondaryCta: 'Book System Demo',
      secondaryCtaUrl: '/contact',
      stats: JSON.stringify([
        { value: '99.999%', label: 'On-Air Availability', description: 'Dual 1+1 Mirror & N+M failover redundancy' },
        { value: '< 1 Frame', label: 'Switching Latency', description: 'Zero-drop hardware baseband SDI & NDI 6' },
        { value: '4K / UHD / HDR', label: 'Format Support', description: 'Full 1080i50, 1080p60, 2160p50/60 pipelines' },
        { value: '15+ Modules', label: 'Ecosystem Breadth', description: 'Playout, CG, MAM, NRCS, Ingest, Switcher' }
      ])
    }
  });

  // 2. Site Settings
  await prisma.siteSetting.createMany({
    data: [
      { key: 'site_name', value: 'Kashtrix Broadcast Systems' },
      { key: 'site_tagline', value: 'Professional Broadcast Playout & Production Suite' },
      { key: 'contact_email', value: 'broadcast@kashtrix.com' },
      { key: 'contact_phone', value: '+1 (800) 527-4874' },
      { key: 'office_location', value: 'Broadcast Engineering Center • Silicon Valley & Worldwide' },
      { key: 'live_status', value: 'ONLINE • ST 2110 / SDI / NDI ENGINE READY' }
    ]
  });

  // 3. Testimonials / Broadcaster Case Studies
  await prisma.testimonial.createMany({
    data: [
      {
        author: 'Marcus Vance',
        station: 'Apex 24 News Network',
        role: 'Chief Broadcast Engineer',
        quote:
          'Migrating our 6 regional channels to Kashtrix reduced our master control room hardware footprint by 65%. The seamless MOS 2.8.4 newsroom integration and instant CG updates during breaking news are second to none.',
        rating: 5,
        order: 1
      },
      {
        author: 'Elena Rostova',
        station: 'EuroSat Sports HD',
        role: 'Head of Transmission & Playout',
        quote:
          'Kashtrix Playout handled 42 days of uninterrupted 24/7 high-density live sports broadcasts without dropping a single frame. The automated SCTE-35 commercial insertion and N+M redundancy give our operators absolute confidence.',
        rating: 5,
        order: 2
      },
      {
        author: 'Rajiv Sharma',
        station: 'Vision One Media Group',
        role: 'Director of Technology',
        quote:
          'The CG Editor with 3D geometry and real-time data bindings eliminated our need for third-party graphics engines. Everything from lower thirds to stock tickers is managed directly within the Kashtrix ecosystem.',
        rating: 5,
        order: 3
      }
    ]
  });

  // 4. Products
  const productsData = [
    {
      slug: 'playout',
      name: 'Kashtrix Playout Engine',
      tagline: 'Enterprise 24/7 Multi-Format Channel Automation & Playout',
      category: 'Core Playout',
      badge: 'FLAGSHIP ENGINE',
      accentColor: '#8055B3',
      icon: 'Tv',
      order: 1,
      featured: true,
      heroImage: '/assets/products/playout-master-rundown.png',
      summary:
        'Rock-solid 24/7 channel automation engine featuring Program & Preview buses, multi-layer CG compositing, SCTE-35 digital cue triggers, secondary event automation, and hardware-accelerated playback.',
      description:
        'Kashtrix Playout Engine is engineered for relentless 24/7 master control transmission. It combines an ultra-low latency playback pipeline with full video and audio processing, live inputs, secondary graphics triggering, automated commercial break insertion, and as-run reconciliation. Supported formats include XDCAM HD 422, ProRes, AVC-Intra, DNxHD, HEVC, and H.264 across SDI, NDI 6, and IP transport streams.',
      specs: JSON.stringify({
        'Video Architecture': 'DirectX 11 / Direct3D Hardware Accelerated Pipeline',
        'Output Standards': 'SMPTE 292M (1.5G SDI), 424M (3G SDI), SMPTE ST 2110, NDI 6, DVB-TS',
        'Audio Channels': 'Up to 16 channels per stream, EBU R128 Loudness Normalization',
        'Automation Protocols': 'VDCP, MOS 2.8.4, BXF, REST API, WebSocket, SNMP v2c',
        'Ad Insertion': 'SCTE-35 TS splicing & SCTE-104 VANC insertion',
        'Redundancy': 'Active 1+1 Mirror and N+M Cluster Failover'
      }),
      features: [
        {
          title: 'Independent Program & Preview Buses',
          description:
            'Operators can cue, scrub, inspect technical metadata, and verify audio phase on Preview without ever affecting on-air transmission.',
          icon: 'MonitorPlay'
        },
        {
          title: 'SCTE-35 & SCTE-104 Digital Insertion',
          description:
            'Frame-accurate digital program insertion triggers embedded in transport streams and baseband ANC for automated downstream ad replacement.',
          icon: 'Zap'
        },
        {
          title: 'Hardware Baseband & IP Streaming',
          description:
            'Simultaneous output to Blackmagic DeckLink SDI, Matrox DSX, AJA Kona, NDI 6, SRT, RTMP, and DVB-TS UDP multicast with full SI/PSI tables.',
          icon: 'Radio'
        },
        {
          title: 'Automated As-Run & Traffic Reconciliation',
          description:
            'Actual transmission timestamps, dropped frames, and operator overrides are permanently recorded for automated traffic reconciliation.',
          icon: 'FileCheck'
        }
      ],
      screenshots: [
        {
          title: 'Master Rundown & Playout Surface',
          caption: 'Operator rundown view with live on-air status, cued items, dual VU meters, and channel timing controls.',
          imagePath: '/assets/products/playout-master-rundown.png',
          category: 'Interface',
          order: 1
        },
        {
          title: 'Preview Bus & Program Switcher',
          caption: 'Preview video player showing clip metadata, audio waveforms, and dynamic lower third triggers.',
          imagePath: '/assets/products/playout-preview-bus.png',
          category: 'Operation',
          order: 2
        },
        {
          title: 'Integrated Media Explorer',
          caption: 'Fast media root explorer with instant search, technical probe, and drag-and-drop rundown insertion.',
          imagePath: '/assets/products/playout-media-explorer.png',
          category: 'Media Browser',
          order: 3
        },
        {
          title: 'Web Remote Playout Client',
          caption: 'Responsive web application providing complete playout monitoring and control from any modern browser.',
          imagePath: '/assets/products/playout-web-client.png',
          category: 'Web Suite',
          order: 4
        }
      ]
    },
    {
      slug: 'studio',
      name: 'Kashtrix Studio',
      tagline: 'Professional Live Production Switcher & Multi-Source Broadcaster',
      category: 'Live Production',
      badge: 'LIVE SWITCHER',
      accentColor: '#E05B5B',
      icon: 'Video',
      order: 2,
      featured: true,
      heroImage: '/assets/products/studio-switcher-full.png',
      summary:
        'Integrated live studio switcher featuring 12-channel multiview, hardware DVE transitions, dual DDR video servers, audio mixer with parametric EQ, graphics overlays, and simultaneous RTMP/SRT streaming & master recording.',
      description:
        'Kashtrix Studio turns your workstation into a multi-camera live broadcast control room. It orchestrates camera inputs, SDI/NDI feeds, DDR playback, stinger transitions, graphic overlays, and live audio mixing with zero drop-frames. Designed for news studios, talk shows, sports broadcasts, and hybrid digital events.',
      specs: JSON.stringify({
        'Video Inputs': 'Up to 12 simultaneous live cameras (SDI, NDI, USB3, SRT)',
        'Transition Engine': 'Hardware Cut, Auto Dissolve, Dip, Wipe, 3D DVE, Custom Stinger Video',
        'Audio Mixing': '16-Bus mixer with 3-Band Parametric EQ, Compressor, De-Esser, Noise Gate',
        'Media Bins': 'Dual DDR clip players, 4 Still stores, 4 Graphic overlay channels',
        'Recording': 'Simultaneous Master ProRes/XDCAM + MP4 Proxy recording with timecode',
        'Streaming': 'Multi-destination RTMP, SRT caller/listener, RTSP, NDI output'
      }),
      features: [
        {
          title: '12-Channel Live Multiview Wall',
          description:
            'Interactive confidence monitor wall with tally borders, dynamic source labeling, audio peak meters, and single-click preview routing.',
          icon: 'LayoutGrid'
        },
        {
          title: 'Broadcast Transition & DVE Engine',
          description:
            'Frame-accurate transitions with variable rate controls, picture-in-picture (PiP), 2D/3D squeeze, and alpha-stinger video playback.',
          icon: 'Sliders'
        },
        {
          title: 'Professional Integrated Audio Console',
          description:
            'Complete software mixing console with individual gain, 220Hz - 4.2kHz EQ, compression ratio, threshold, and master limiter.',
          icon: 'Volume2'
        },
        {
          title: 'Direct Stream & Master Record',
          description:
            'One-click streaming to YouTube, Facebook, Twitch, and enterprise CDNs while recording an uncompressed broadcast master.',
          icon: 'Radio'
        }
      ],
      screenshots: [
        {
          title: 'Live Production Studio Console',
          caption: 'Full studio interface displaying multiview wall, Program/Preview buses, transition controls, audio mixer, and stream status.',
          imagePath: '/assets/products/studio-switcher-full.png',
          category: 'Interface',
          order: 1
        },
        {
          title: 'Camera Bus & DVE Transition Control',
          caption: 'High-visibility bus selector with active camera tallies, dissolve rates, and DDR media delegates.',
          imagePath: '/assets/products/studio-switcher-pvw-pgm.png',
          category: 'Operation',
          order: 2
        }
      ]
    },
    {
      slug: 'cg-editor',
      name: 'Kashtrix CG Editor',
      tagline: 'Visual Broadcast Motion Graphics Template Designer & Compositor',
      category: 'Graphics & CG',
      badge: '3D COMPOSITOR',
      accentColor: '#B76BA8',
      icon: 'Layers',
      order: 3,
      featured: true,
      heroImage: '/assets/products/cg-editor-main.png',
      summary:
        'Author stunning broadcast graphics templates with multi-track keyframe timelines, 3D geometry transforms, bezier easing curves, rich typography, web HTML layers, and live data bindings.',
      description:
        'Kashtrix CG Editor is a standalone authoring tool for broadcast motion graphics artists. Design lower thirds, tickers, breaking news bugs, full-frame slabs, sports scoreboards, weather graphics, and election visuals. Templates expose dynamic fields that can be driven in real time by newsroom journalists, external JSON APIs, RSS feeds, or spreadsheet databases.',
      specs: JSON.stringify({
        'Compositing Engine': 'Hardware-accelerated 2D/3D compositor with perspective transforms',
        'Object Types': 'Text, Shapes, Pen Vector, Ticker, Roll, Clock, Weather, Image Sequence, Video, HTML/Web',
        'Animation': 'Multi-track timeline with cubic-bezier easing curves, keyframe interpolation',
        'Color & Styling': 'Custom broadcast color picker, linear/radial gradients, glows, drop shadows, borders',
        'Data Dynamic Fields': 'JSON, REST APIs, XML, CSV, MOS Object bindings with live reload'
      }),
      features: [
        {
          title: '3D Geometry & Transforms',
          description:
            'Animate position, scale, rotation in X/Y/Z planes, perspective depth, skew, and anchor point offsets with sub-pixel precision.',
          icon: 'Box'
        },
        {
          title: 'Smooth Bezier Curve Animation',
          description:
            'Fine-tune motion dynamics with graphical cubic-bezier velocity curves, smooth acceleration presets, and loop cycles.',
          icon: 'Activity'
        },
        {
          title: 'Dynamic Data & MOS Bindings',
          description:
            'Expose template fields like headline, subtitle, reporter name, and live stock data for zero-code updates from the control room.',
          icon: 'Database'
        },
        {
          title: 'HTML & Web Layer Integration',
          description:
            'Embed live web widgets, reactive charts, interactive social media feeds, and custom HTML5 canvases directly in broadcast templates.',
          icon: 'Globe'
        }
      ],
      screenshots: [
        {
          title: 'CG Broadcast Editor Workspace',
          caption: 'Template authoring canvas with layer hierarchy, 3D geometry transform controls, and visual shape tools.',
          imagePath: '/assets/products/cg-editor-main.png',
          category: 'Workspace',
          order: 1
        },
        {
          title: 'Compositor Canvas & Live Preview',
          caption: 'Real-time rendering canvas displaying multi-layer news package graphics and alpha transparency.',
          imagePath: '/assets/products/cg-editor-compositor.png',
          category: 'Canvas',
          order: 2
        },
        {
          title: 'Inspector & Layer Effects',
          caption: 'Comprehensive properties inspector for gradients, shadows, stroke weight, blur, and opacity blending.',
          imagePath: '/assets/products/cg-editor-inspector.png',
          category: 'Properties',
          order: 3
        },
        {
          title: 'Typography & Text Styling',
          caption: 'Advanced typography panel with font family selector, character tracking, kerning, line height, and text shadows.',
          imagePath: '/assets/products/cg-editor-typography.png',
          category: 'Typography',
          order: 4
        },
        {
          title: 'Multi-Track Keyframe Timeline',
          caption: 'Non-linear timeline displaying keyframe marks, duration markers, in/out animations, and easing curves.',
          imagePath: '/assets/products/cg-editor-timeline.png',
          category: 'Timeline',
          order: 5
        }
      ]
    },
    {
      slug: 'cg-controller',
      name: 'Kashtrix CG Controller',
      tagline: 'Live On-Air Graphics Management & Preview/Program Control Surface',
      category: 'Graphics & CG',
      badge: 'LIVE ON-AIR CG',
      accentColor: '#B78BE7',
      icon: 'Sparkles',
      order: 4,
      featured: true,
      heroImage: '/assets/products/cg-controller-main.png',
      summary:
        'Tactile live graphics control panel allowing operators to select templates, edit field data on the fly, verify on independent Preview bus, and execute Take, Update, or Clear commands onto Program.',
      description:
        'Kashtrix CG Controller bridges graphics authoring and live transmission. Operators can target single or multiple playout channels simultaneously. Update live tickers, trigger breaking news banners, cue sports scores, and manage multi-layer graphic stacks with independent preview verification and instant live Take.',
      specs: JSON.stringify({
        'Bus Architecture': 'Dual Independent Preview and Program Graphics Buses',
        'Channel Addressing': 'Single channel or multi-channel synchronized trigger via TCP/IP',
        'Update Latency': '< 16ms instant live data update without restarting in-flight animations',
        'Layer Stacking': 'Up to 32 simultaneous graphic layers with priority z-ordering',
        'Control Interfaces': 'WPF Desktop Console, Web Client (Port 5174), StreamDeck, REST API'
      }),
      features: [
        {
          title: 'Dual Bus Preview/Program Operation',
          description:
            'Preview full compositions and data replacements before pushing to live broadcast. Ensure zero on-air typos or styling errors.',
          icon: 'Eye'
        },
        {
          title: 'Quick Edit Dynamic Lower Thirds',
          description:
            'Dedicated fast-entry panel for rapid headline and caption typing during breaking news and live press conferences.',
          icon: 'Edit3'
        },
        {
          title: 'Independent Layer Clearing & Updates',
          description:
            'Update existing on-screen graphics data without disrupting ongoing motion. Stop or clear individual layers or all graphics at once.',
          icon: 'Layers'
        },
        {
          title: 'Multi-Channel Graphics Distribution',
          description:
            'Broadcast identical or localized graphics templates across multiple regional playout transmitters from one workstation.',
          icon: 'Share2'
        }
      ],
      screenshots: [
        {
          title: 'Multi-Overlay Graphics Playback',
          caption: 'Main CG Controller panel showing template library, live on-air layers, and Preview/Program Take buttons.',
          imagePath: '/assets/products/cg-controller-main.png',
          category: 'Interface',
          order: 1
        },
        {
          title: 'Quick Edit Dynamic Lower Thirds',
          caption: 'Operator quick-entry dialog for typing breaking news titles and immediate on-air lower third firing.',
          imagePath: '/assets/products/cg-controller-quickedit.png',
          category: 'Operation',
          order: 2
        },
        {
          title: 'Active On-Air Layers Stack',
          caption: 'Real-time status of all graphics currently visible on Program with individual duration timers.',
          imagePath: '/assets/products/cg-controller-layers.png',
          category: 'Layers',
          order: 3
        },
        {
          title: 'Dynamic Data Bindings Panel',
          caption: 'Field mapping table linking template placeholders to live channel metadata feeds.',
          imagePath: '/assets/products/cg-controller-bindings.png',
          category: 'Data',
          order: 4
        }
      ]
    },
    {
      slug: 'mam',
      name: 'Kashtrix MAM',
      tagline: 'Enterprise Media Asset Management, Metadata & Automated QC',
      category: 'Asset Management',
      badge: 'ENTERPRISE MAM',
      accentColor: '#55C878',
      icon: 'Database',
      order: 5,
      featured: true,
      heroImage: '/assets/products/mam-catalog.png',
      summary:
        'Comprehensive broadcast media catalogue featuring permanent GUID asset tracking, automated proxy generation, SHA-256 duplicate detection, broadcast QC validation, and deep metadata management.',
      description:
        'Kashtrix MAM (Media Asset Management) serves as the central nervous system for all broadcast media. It handles asset registration, automated technical analysis, proxy generation, checksum integrity, rights windows, and archive lifecycle. Playout, Ingest, and Newsroom modules link directly to MAM assets with zero manual re-entry.',
      specs: JSON.stringify({
        'Identification': 'Permanent UUID/GUID per asset with version tree and component tracking',
        'Checksum Integrity': 'Real-time SHA-256 hash generation and duplicate file detection',
        'Proxy Pipeline': 'Automated H.264 MP4 proxy creation with burn-in timecode',
        'Technical Metadata': 'Audio track mapping, caption streams, codec profiles, color space, aspect ratio',
        'Storage Integration': 'SAN, NAS, S3 Cloud Object Storage, LTO Tape archive connectors'
      }),
      features: [
        {
          title: 'Permanent Asset GUID Tracking',
          description:
            'Every ingested file receives an immutable identifier with complete lineage, version history, and relations.',
          icon: 'Fingerprint'
        },
        {
          title: 'Automated Proxy & Contact Sheets',
          description:
            'Generates lightweight browser-streamable proxies and storyboard thumbnails for instant journalist browsing.',
          icon: 'Film'
        },
        {
          title: 'SHA-256 Checksum Validation',
          description:
            'Ensures media integrity during ingest, transfer, playout caching, and long-term archive retrieval.',
          icon: 'ShieldCheck'
        },
        {
          title: 'Broadcast QC & Compliance Gating',
          description:
            'Assets that fail quality control or loudness standards are automatically blocked from entering on-air rundowns.',
          icon: 'CheckSquare'
        }
      ],
      screenshots: [
        {
          title: 'Media Asset Catalogue & Metadata Explorer',
          caption: 'Central asset management view showing file status, format specifications, audio channels, and GUIDs.',
          imagePath: '/assets/products/mam-catalog.png',
          category: 'Catalogue',
          order: 1
        },
        {
          title: 'MAM Storage & Archival Settings',
          caption: 'Configuration interface for storage volumes, proxy encoding presets, and archive lifecycle policies.',
          imagePath: '/assets/products/mam-storage-settings.png',
          category: 'Settings',
          order: 2
        }
      ]
    },
    {
      slug: 'ingest-server',
      name: 'Kashtrix Ingest Server',
      tagline: 'Multi-Source Scheduled, Compliance & Live Ingest Capture',
      category: 'Asset Management',
      badge: 'MULTI-CHANNEL INGEST',
      accentColor: '#C68645',
      icon: 'HardDrive',
      order: 6,
      featured: true,
      heroImage: '/assets/products/ingest-server-main.png',
      summary:
        'Multi-channel broadcast capture engine supporting SDI, NDI, and SRT inputs. Features scheduled recurring ingest, compliance 24/7 logging, crash recording, and instant MAM registration.',
      description:
        'Kashtrix Ingest Server delivers reliable multi-input video capture for live satellite feeds, OB trucks, studio cameras, and network links. Choose broadcast master profiles (XDCAM HD 422, ProRes, DNxHD) with simultaneous proxy generation and automatic segmenting.',
      specs: JSON.stringify({
        'Capture Interfaces': 'Blackmagic DeckLink SDI, Matrox DSX, NDI 6, SRT Caller/Listener, RTSP',
        'Recording Profiles': 'Master (MPEG-2 50Mbps 4:2:2, ProRes 422 HQ, DNxHD) + Simultaneous Proxy',
        'Scheduling': 'One-time, recurring daily/weekly, or continuous 30-day compliance logging',
        'Timecode Policy': 'LTC input, SDI VANC ATC, or system clock synchronization',
        'Storage Fail-Safe': 'Low-disk thresholds, automatic rollover, and source-loss recovery'
      }),
      features: [
        {
          title: 'Scheduled & Crash Recording',
          description:
            'Schedule feed captures in advance with calendar recurrence or trigger instant crash recording for breaking news.',
          icon: 'Calendar'
        },
        {
          title: 'Live Input Grid Monitoring',
          description:
            'Inspect multi-source incoming feeds with real-time video frames, audio peak meters, and lock status.',
          icon: 'Grid'
        },
        {
          title: 'Instant Automated MAM Handoff',
          description:
            'Captured recordings are instantly registered into the MAM catalogue with technical metadata and proxy previews.',
          icon: 'RefreshCw'
        },
        {
          title: 'Compliance 24/7 Logging',
          description:
            'Low-bitrate continuous logging with burn-in date/time and loudness metrics for legal broadcasting requirements.',
          icon: 'Clock'
        }
      ],
      screenshots: [
        {
          title: 'Ingest Server Multi-Channel Console',
          caption: 'Active recording sessions, timecode counters, input format locks, and scheduled calendar queue.',
          imagePath: '/assets/products/ingest-server-main.png',
          category: 'Interface',
          order: 1
        },
        {
          title: 'Multi-Source Live Input Grid',
          caption: 'Real-time monitoring grid displaying all baseband and IP video sources connected to the server.',
          imagePath: '/assets/products/input-engine-grid.png',
          category: 'Input Grid',
          order: 2
        }
      ]
    },
    {
      slug: 'nrcs',
      name: 'Kashtrix NRCS / MOS Newsroom',
      tagline: 'Enterprise Newsroom Computer System with MOS 2.8.4 Protocol',
      category: 'Newsroom & MOS',
      badge: 'ENTERPRISE NEWSROOM',
      accentColor: '#D9AE55',
      icon: 'FileText',
      order: 7,
      featured: true,
      heroImage: '/assets/products/nrcs-enterprise-overview.png',
      summary:
        'Complete television newsroom workflow system. Author stories, build bulletin rundowns, attach MAM media and CG graphics as MOS objects, and push live changes directly to Playout and Teleprompter.',
      description:
        'Kashtrix NRCS integrates the entire newsroom lifecycle from assignment to on-air transmission. Journalists author stories with prompter copy, link approved video clips from MAM, and fill in lower-third templates without leaving their editor. The integrated MOS 2.8.4 server connects to external news systems or operates as an all-in-one newsroom hub.',
      specs: JSON.stringify({
        'MOS Version': 'Full MOS 2.8.4 compliance (mosObj, mosReqObj, roCreate, roStorySend, roElementAction)',
        'Collaboration': 'Multi-user concurrent rundown editing with row-locking and real-time push',
        'Prompter Sync': 'Native bidirectional synchronization with Kashtrix Teleprompter',
        'Timing Engine': 'Planned vs actual story duration, back-timing to top of hour, auto-cume calculation',
        'Story Bank': 'Topic categories, wire service feeds, story templates, and archived bulletin search'
      }),
      features: [
        {
          title: 'MOS 2.8.4 Object Attachment',
          description:
            'Attach MAM video clips and CG graphics templates directly to rundown story items using standardized MOS transactions.',
          icon: 'Paperclip'
        },
        {
          title: 'Live Story Authoring & Prompter Sync',
          description:
            'Journalists write story copy once; the live prompter follows rundown order and receives instant late-breaking edits.',
          icon: 'Type'
        },
        {
          title: 'Precision Back-Timing & Overrun Warning',
          description:
            'Automated bulletin timing computes remaining time to commercial breaks or top-of-the-hour transmitter cutovers.',
          icon: 'Clock'
        },
        {
          title: 'Enterprise News Rundown Bank',
          description:
            'Multi-bulletin management across morning, afternoon, and primetime news teams with category-coded stories.',
          icon: 'Folder'
        }
      ],
      screenshots: [
        {
          title: 'Enterprise Newsroom Overview',
          caption: 'Bulletin rundown grid showing story items, segment status, attached MOS objects, and cumulative timing.',
          imagePath: '/assets/products/nrcs-enterprise-overview.png',
          category: 'Rundown',
          order: 1
        },
        {
          title: 'Integrated Newsroom Story Workflow',
          caption: 'Story authoring view with prompter text script, MOS object inspector, and video playback thumbnail.',
          imagePath: '/assets/products/nrcs-newsroom-workflow.png',
          category: 'Workflow',
          order: 2
        }
      ]
    },
    {
      slug: 'prompter',
      name: 'Kashtrix Teleprompter',
      tagline: 'Broadcast Studio Teleprompter with Live NRCS Synchronization',
      category: 'Newsroom & MOS',
      badge: 'STUDIO PROMPTER',
      accentColor: '#55C878',
      icon: 'TvMinimalPlay',
      order: 8,
      featured: true,
      heroImage: '/assets/products/prompter-live-studio.png',
      summary:
        'Professional studio teleprompter engine with ultra-smooth scrolling, talent cue markers, bidirectional NRCS story synchronization, and wireless foot-pedal/hand controller support.',
      description:
        'Kashtrix Prompter delivers an uncompromised reading experience for on-air news anchors and studio talent. Text rendering is GPU-accelerated to prevent any judder or micro-stutter at any speed. Dual display outputs provide mirrored on-camera monitor feeds alongside operator preview displays.',
      specs: JSON.stringify({
        'Display Modes': 'Horizontal and vertical mirror flipping for on-camera beamsplitter glass',
        'Scroll Engine': 'Hardware GPU rasterization with micro-step variable velocity curves',
        'Hardware Controls': 'USB foot pedals, ShuttleXpress, wireless remotes, keyboard shortcuts',
        'NRCS Integration': 'Automatic real-time story re-ordering and in-script breaking news updates',
        'Talent Cues': 'Anchor indicators, elapsed segment timers, cue lines, slug labels'
      }),
      features: [
        {
          title: 'Direct NRCS Story Synchronization',
          description:
            'Follows the live news rundown automatically. When producers move or insert stories, the prompter updates instantly.',
          icon: 'Zap'
        },
        {
          title: 'Beamsplitter Mirror Flip',
          description:
            'Single-click mirror inversion for standard through-the-lens studio teleprompter glass hoods.',
          icon: 'FlipHorizontal'
        },
        {
          title: 'Smooth Variable Speed Scrolling',
          description:
            'Fluid scroll control with configurable acceleration curves, pause markers, and story-skip hotkeys.',
          icon: 'Sliders'
        },
        {
          title: 'Talent Readability Display',
          description:
            'High-contrast broadcast typography with customizable slug colors, eye-line guide markers, and timecode.',
          icon: 'Eye'
        }
      ],
      screenshots: [
        {
          title: 'Live Studio Teleprompter Interface',
          caption: 'Operator and talent screen showing scroll progress, story slug headlines, anchor cues, and speed dials.',
          imagePath: '/assets/products/prompter-live-studio.png',
          category: 'Interface',
          order: 1
        }
      ]
    },
    {
      slug: 'channel-controller',
      name: 'Kashtrix Channel Controller',
      tagline: 'Multi-Channel Playout Orchestrator & Station Cluster Manager',
      category: 'Monitoring & I/O',
      badge: 'MULTI-STATION CLUSTER',
      accentColor: '#8055B3',
      icon: 'Network',
      order: 9,
      featured: true,
      heroImage: '/assets/products/channel-controller-cluster.png',
      summary:
        'Centralized control surface to supervise and command tens or hundreds of remote playout channels over TCP/IP. Group commands, unified status, and individual per-channel feedback.',
      description:
        'For broadcast networks operating multiple national and regional channels, Kashtrix Channel Controller offers single-pane-of-glass supervision. View on-air items, next events, alarm states, output armed status, and execute group commands with per-channel verification.',
      specs: JSON.stringify({
        'Network Protocol': 'TCP JSONL persistent socket on port 9120 with heartbeat verification',
        'Capacity': 'Supervise up to 256 concurrent playout engine nodes per cluster',
        'Grouping': 'Define channel groups (e.g. National, Regional, Sports, News) for batch operations',
        'Telemetry': 'Live on-air item, next cued item, buffer levels, decoder load, redundancy state'
      }),
      features: [
        {
          title: 'Cluster Supervision Grid',
          description:
            'Monitor status of every connected transmitter: active/standby mode, alarm count, output armed state, and channel health.',
          icon: 'Server'
        },
        {
          title: 'Synchronized Multi-Channel Commands',
          description:
            'Trigger coordinated Take, Next, or Emergency Hold commands across multiple channels with per-channel outcome auditing.',
          icon: 'CheckCircle'
        },
        {
          title: 'Zero Single-Point-of-Failure',
          description:
            'Channel Controller acts as an orchestration surface; individual playout engines run autonomously if the network disconnects.',
          icon: 'Shield'
        }
      ],
      screenshots: [
        {
          title: 'Multi-Channel Cluster Orchestration Grid',
          caption: 'Aggregated view of remote playout engines listening on TCP port 9120 with status indicators and command bars.',
          imagePath: '/assets/products/channel-controller-cluster.png',
          category: 'Orchestrator',
          order: 1
        }
      ]
    },
    {
      slug: 'output-engine',
      name: 'Kashtrix Output Engine',
      tagline: 'Multi-Protocol Hardware Baseband, IP Streaming & DVB SI/PSI Engine',
      category: 'Monitoring & I/O',
      badge: 'DVB & BASEBAND',
      accentColor: '#B76BA8',
      icon: 'Radio',
      order: 10,
      featured: true,
      heroImage: '/assets/products/output-engine-baseband.png',
      summary:
        'Comprehensive broadcast transmission output supporting Blackmagic DeckLink, Matrox DSX, AJA, NDI 6, SRT, RTMP, and DVB-TS UDP with full PSI/SI tables (PAT, PMT, SDT, NIT, TDT).',
      description:
        'Kashtrix Output Engine delivers pristine broadcast video to satellite uplinks, cable headends, DTT transmitters, and global CDN streaming points. It combines hardware baseband SDI output with compliant MPEG Transport Streams, hardware GPU encoders (NVENC, Intel QSV, AMD AMF), and multi-bitrate streaming.',
      specs: JSON.stringify({
        'Baseband Hardware': 'Blackmagic Design DeckLink / UltraStudio, Matrox DSX LE/X.mio, AJA Kona / Corvid',
        'IP Broadcast': 'SMPTE ST 2110-20/30/40, ST 2022-7 Hitless Redundancy, NDI 6 (16-bit HDR)',
        'Transport Stream': 'DVB-TS UDP Unicast/Multicast with full PSI/SI tables (PAT, PMT, SDT, NIT, TDT, EIT)',
        'Hardware Encoding': 'NVIDIA NVENC H.264/HEVC, Intel QuickSync QSV, AMD AMF, x264/x265 CPU fallback'
      }),
      features: [
        {
          title: 'DVB-TS Full PSI/SI Compliance',
          description:
            'Compliant transport stream multiplexing including Program Association Table, PMT, SDT, and network identifiers.',
          icon: 'Cpu'
        },
        {
          title: 'Multi-Protocol Output Matrix',
          description:
            'Simultaneously output to SDI, NDI, SRT caller/listener, and RTMP without performance degradation.',
          icon: 'Share2'
        },
        {
          title: 'Hardware GPU Video Encoding',
          description:
            'High-throughput NVENC and QSV acceleration with configurable bitrates, GOP structure, and lookahead buffering.',
          icon: 'Zap'
        }
      ],
      screenshots: [
        {
          title: 'Hardware Baseband & DVB Output Configuration',
          caption: 'Output engine interface showing DeckLink SDI device selector, DVB SI/PSI parameters, and bitrate encoders.',
          imagePath: '/assets/products/output-engine-baseband.png',
          category: 'Output',
          order: 1
        }
      ]
    },
    {
      slug: 'multiview',
      name: 'Kashtrix Multiview & Monitoring',
      tagline: 'Broadcast Multi-Source Confidence Wall, Loudness Radar & Signal Alarms',
      category: 'Monitoring & I/O',
      badge: 'MONITOR WALL',
      accentColor: '#55C878',
      icon: 'LayoutGrid',
      order: 11,
      featured: true,
      heroImage: '/assets/products/multiview-wall-qc.png',
      summary:
        'Scalable confidence monitor wall displaying all channel outputs, live cameras, and server feeds with multi-channel audio VU meters, black/freeze detection, and loudness monitoring.',
      description:
        'Kashtrix Multiview provides master control operators with instantaneous confidence monitoring. Set up custom multiview layouts (1x2, 2x2, 3x3, 4x4, custom PiP) with audio bar graphs, digital clocks, tally markers, and automated visual alarms for freeze-frame, black-screen, or audio loss.',
      specs: JSON.stringify({
        'Grid Layouts': 'Fully flexible 1-up to 16-up layouts with custom aspect ratio scaling',
        'Audio Monitoring': 'Dual and multi-channel peak VU meter overlays per tile with clip warnings',
        'Alarm Detectors': 'Video Freeze, Black Screen, Audio Silence, Signal Loss, Decoder Stutter',
        'Display Outputs': 'Multi-monitor support across HDMI, DisplayPort, SDI, and NDI streams'
      }),
      features: [
        {
          title: 'Multi-Source Confidence Wall',
          description:
            'Real-time visualization of Program, Preview, Live Ingest, and Remote Feeds on a unified low-latency screen.',
          icon: 'Grid'
        },
        {
          title: 'Automated Signal Alarms',
          description:
            'Visual tally borders flash amber or red upon audio silence, frozen frames, or signal carrier drop.',
          icon: 'AlertTriangle'
        },
        {
          title: 'EBU R128 Audio Loudness Meters',
          description:
            'Calibrated audio metering adhering to international broadcast loudness and headroom standards.',
          icon: 'Volume2'
        }
      ],
      screenshots: [
        {
          title: 'Broadcast Multi-Source Wall & QC Engine',
          caption: 'Multiview confidence wall displaying channel outputs, audio meters, source labels, and QC telemetry.',
          imagePath: '/assets/products/multiview-wall-qc.png',
          category: 'Multiview',
          order: 1
        },
        {
          title: 'Channel Wall Confidence Monitoring',
          caption: 'Dedicated 4-channel master wall layout with synchronized studio timecode clock.',
          imagePath: '/assets/products/multiview-monitoring.png',
          category: 'Monitoring',
          order: 2
        }
      ]
    },
    {
      slug: 'scheduler',
      name: 'Kashtrix Scheduler & Traffic',
      tagline: '24/7 Broadcast Channel Automation, BXF Import & Commercial Breaks',
      category: 'Core Playout',
      badge: '24/7 SCHEDULER',
      accentColor: '#D9AE55',
      icon: 'Calendar',
      order: 12,
      featured: true,
      heroImage: '/assets/products/scheduler-24-7.png',
      summary:
        'Advanced broadcast scheduling and playlist management with BXF traffic exchange, day timeline views, follow/fixed timing modes, commercial break editors, and as-run reconciliation.',
      description:
        'Kashtrix Scheduler powers 24/7 linear television scheduling. Import commercial playlists from external traffic management systems via BXF or CSV, insert automated promotional blocks, assign graphic templates to specific categories, and maintain accurate transmission timing around the clock.',
      specs: JSON.stringify({
        'Timing Modes': 'Auto-Follow, Hard Start Time, Soft Start Time, Manual Hold, Top of Hour Trigger',
        'Traffic Protocols': 'SMPTE 2021 BXF XML Import/Export, Flat CSV, Custom Schedule Parsers',
        'Break Insertion': 'Dedicated Commercial Break Editor with secondary graphic trigger bindings',
        'EPG Generation': 'Automatic XMLTV electronic program guide export with custom columns'
      }),
      features: [
        {
          title: '24/7 Linear Day Timeline',
          description:
            'Visual day timeline showing scheduled programs, commercial breaks, live feed windows, and filler blocks.',
          icon: 'Clock'
        },
        {
          title: 'BXF Traffic Import & Reconciliation',
          description:
            'Import schedule files directly from traffic systems and export reconciled as-run logs with actual broadcast times.',
          icon: 'FileText'
        },
        {
          title: 'Dedicated Playlist Rundown Editor',
          description:
            'Color-coded category views for News, Movies, Commercials, Promos, and Music with instant duration calculations.',
          icon: 'List'
        }
      ],
      screenshots: [
        {
          title: '24/7 Broadcast Channel Scheduler',
          caption: 'Day timeline schedule view showing programmed blocks, start times, durations, and category colors.',
          imagePath: '/assets/products/scheduler-24-7.png',
          category: 'Scheduler',
          order: 1
        },
        {
          title: 'Playlist Rundown & Break Editor',
          caption: 'Fine-grained playlist editor for sequencing clips, commercials, and graphics secondary events.',
          imagePath: '/assets/products/playlist-editor-rundown.png',
          category: 'Playlist',
          order: 2
        }
      ]
    },
    {
      slug: 'ha-redundancy',
      name: 'Kashtrix HA & Redundancy',
      tagline: 'High Availability 1+1 Mirror & N+M Seamless Failover Architecture',
      category: 'Monitoring & I/O',
      badge: 'HIGH AVAILABILITY',
      accentColor: '#E05B5B',
      icon: 'ShieldCheck',
      order: 13,
      featured: true,
      heroImage: '/assets/products/ha-redundancy-cluster.png',
      summary:
        'Zero-downtime broadcast redundancy controller. Synchronizes rundowns between primary and hot backup nodes with sub-second heartbeat health monitoring and automatic switchover.',
      description:
        'Transmission failure is not an option in broadcast television. Kashtrix HA Controller continuously monitors primary playout nodes. Backup nodes keep identical playlists, video decoders, and graphics states cued in lockstep. Upon any hardware or process anomaly, transmission switches seamlessly without viewer disruption.',
      specs: JSON.stringify({
        'Redundancy Models': '1+1 Hot Standby Mirror, N+M Shared Backup Capacity Architecture',
        'Heartbeat Rate': 'Configurable 200ms - 500ms UDP multicast heartbeat on port 42420',
        'Failover Latency': '< 1.8 seconds automated transition with manual maintenance override',
        'SMPTE ST 2022-7': 'Hitless A/B network packet redundancy over dual 100GbE NICs'
      }),
      features: [
        {
          title: 'Active 1+1 Mirror & N+M Capacity',
          description:
            'Run dedicated hot standby engines or shared backup clusters that take over any failed channel on demand.',
          icon: 'Server'
        },
        {
          title: 'Continuous Rundown Synchronization',
          description:
            'Edits, trims, additions, and cue positions on the primary engine are mirrored to backup engines in real time.',
          icon: 'RefreshCw'
        },
        {
          title: 'SMPTE ST 2110 & 2022-7 Hitless Redundancy',
          description:
            'Dual destination IP transmission ensuring uninterruptible uncompressed video over dual network fabrics.',
          icon: 'Network'
        }
      ],
      screenshots: [
        {
          title: 'HA Redundancy Controller Cluster',
          caption: 'Redundancy controller console showing cluster node health, heartbeat timing, and active failover state.',
          imagePath: '/assets/products/ha-redundancy-cluster.png',
          category: 'Redundancy',
          order: 1
        },
        {
          title: 'Professional I/O & ST 2110 IP Redundancy',
          caption: 'SMPTE ST 2110, ST 2022-7, NMOS, and SCTE-35 IP transmission configuration interface.',
          imagePath: '/assets/products/pro-io-st2110-scte.png',
          category: 'IP Standards',
          order: 2
        }
      ]
    },
    {
      slug: 'api-gateway',
      name: 'Kashtrix API Gateway & Cloud Suite',
      tagline: 'Unified REST, WebSocket, TCP JSONL & MCP Integration Gateway',
      category: 'Monitoring & I/O',
      badge: 'UNIFIED API BUS',
      accentColor: '#8055B3',
      icon: 'Code2',
      order: 14,
      featured: true,
      heroImage: '/assets/products/api-gateway-unified-bus.png',
      summary:
        'Unified broadcast control bus exposing high-speed REST, WebSocket, TCP JSONL, and MCP interfaces on port 9080. Power modern web clients and custom automation integrations.',
      description:
        'Kashtrix API Gateway provides a modern programmable surface for third-party broadcast automation, web portals, mobile control apps, and AI agent workflows. It translates broadcast events into typed JSON streams with bidirectional commands for Playout, CG, Ingest, and Channel Controller.',
      specs: JSON.stringify({
        'API Protocols': 'HTTP REST (CRUD), WebSocket (Realtime state), TCP JSONL (M2M), MCP Streamable HTTP',
        'Default Ports': '9080 (Unified API Gateway), 5173 (Playout Web), 5174 (CG Web)',
        'Authentication': 'API Key, JWT Bearer Token, Role-Based Access Control (RBAC)',
        'Open Standards': 'Swagger OpenAPI 3.0, Model Context Protocol (MCP) tool integration'
      }),
      features: [
        {
          title: 'Unified Broadcast Bus Architecture',
          description:
            'A single entry point coordinates state across all local and remote Kashtrix services without protocol fragmentation.',
          icon: 'Terminal'
        },
        {
          title: 'Real-Time WebSockets & Telemetry',
          description:
            'Push real-time timecode updates, VU levels, and clip transitions to web interfaces with minimal CPU overhead.',
          icon: 'Radio'
        },
        {
          title: 'Web Playout & CG Remote Apps',
          description:
            'Modern web applications for operators and journalists to control playout and graphics from laptops and tablets.',
          icon: 'Monitor'
        }
      ],
      screenshots: [
        {
          title: 'Kashtrix Unified Broadcast Bus',
          caption: 'API Gateway monitoring console displaying active REST, WebSocket, and TCP client connections.',
          imagePath: '/assets/products/api-gateway-unified-bus.png',
          category: 'API Gateway',
          order: 1
        },
        {
          title: 'REST & WebSocket Endpoint Console',
          caption: 'Interactive API explorer for testing Playout and CG remote control commands over HTTP and WebSockets.',
          imagePath: '/assets/products/api-gateway-endpoints.png',
          category: 'Endpoints',
          order: 2
        },
        {
          title: 'Modular Broadcast Suite Launcher',
          caption: 'Central application launcher hub providing quick access to all Kashtrix desktop and web applications.',
          imagePath: '/assets/products/suite-overview-launcher.png',
          category: 'Suite Hub',
          order: 3
        }
      ]
    },
    {
      slug: 'nrcs-web',
      name: 'Kashtrix NRCS Web',
      tagline: 'Cloud & Web-Based Newsroom Computer System with MOS 2.8.4',
      category: 'Newsroom & MOS',
      badge: 'WEB NEWSROOM',
      accentColor: '#D97706',
      icon: 'FileText',
      order: 15,
      featured: true,
      heroImage: '/assets/products/nrcs-newsroom-workflow.png',
      summary:
        'Modern browser-native newsroom system for journalists, editors, and producers. Collaborate from anywhere with real-time story scripting, MOS 2.8.4 media binding, prompter push, and bulletin rundown management.',
      description:
        'Kashtrix NRCS Web untethers newsroom teams from legacy desktop terminals. Journalists can write, review, and approve stories from field laptops, tablets, or studio workstations. Seamlessly link approved video clips from Kashtrix MAM, attach graphic templates with live lower-third previews via HTML5 plugins, and push rundown changes to Playout and Teleprompter in real time.',
      specs: JSON.stringify({
        'Architecture': 'Cloud-Native / On-Premise Web Application (Docker & Node/Go)',
        'MOS Protocol': 'MOS 2.8.4 HTML5 & ActiveX Plugin Integration (mosObj, roStorySend)',
        'Collaboration': 'Multi-user concurrent rundown editing with row-locking and WebSocket updates',
        'Prompter Sync': 'Native real-time sync with Kashtrix Teleprompter & studio monitors',
        'Timing Engine': 'Real-time word-count to speech timing, back-timing to top-of-hour, auto-cume'
      }),
      features: [
        {
          title: 'Cloud & Browser Story Authoring',
          description:
            'Write news copy, anchor intros, and reporter tags from any modern web browser with instant auto-save and revision history.',
          icon: 'Globe'
        },
        {
          title: 'MOS 2.8.4 Media & CG Attachment',
          description:
            'Search MAM video assets and preview 3D CG lower-thirds directly in the web story editor.',
          icon: 'Paperclip'
        },
        {
          title: 'Real-Time Bulletin Rundowns',
          description:
            'Producers build and sequence rundowns with live segment timing and automated over/under indicators.',
          icon: 'ListOrdered'
        },
        {
          title: 'Instant Teleprompter & Playout Push',
          description:
            'Breaking news updates and story re-orderings update on-air playout and teleprompter displays immediately.',
          icon: 'Zap'
        }
      ],
      screenshots: [
        {
          title: 'Kashtrix NRCS Web Story Editor & Rundown',
          caption: 'Web story authoring interface with real-time script timing, MAM asset attachments, and live prompter synchronization.',
          imagePath: '/assets/products/nrcs-newsroom-workflow.png',
          category: 'Story Editor',
          order: 1
        },
        {
          title: 'Enterprise Bulletin Rundown Manager',
          caption: 'Multi-bulletin overview showing segment status, assigned reporters, MOS objects, and target block durations.',
          imagePath: '/assets/products/nrcs-enterprise-overview.png',
          category: 'Rundown',
          order: 2
        }
      ]
    },
    {
      slug: 'streamops',
      name: 'StreamOps',
      tagline: 'Cloud & Hybrid Live Streaming, Multi-CDN Transcoder & OTT Distribution',
      category: 'Streaming & OTT',
      badge: 'OTT & MULTI-CDN',
      accentColor: '#0284C7',
      icon: 'Radio',
      order: 16,
      featured: true,
      heroImage: '/assets/products/pro-io-st2110-scte.png',
      summary:
        'High-density multi-profile live video transcoder and OTT distribution engine. Deliver pristine low-latency HLS, MPEG-DASH, SRT, and RTMP streams across multiple CDNs with SCTE-35 digital ad insertion.',
      description:
        'StreamOps bridges broadcast baseband playout and the global OTT streaming ecosystem. Built for linear TV channels, FAST channels, sports networks, and live events, StreamOps ingests uncompressed SDI, ST 2110, or NDI feeds and outputs adaptive bitrate ladders with hardware NVENC/QuickSync acceleration. Features include server-side ad insertion (SSAI) signaling, EPG XMLTV packaging, DRM encryption, and multi-CDN failover routing.',
      specs: JSON.stringify({
        'Transcoding Acceleration': 'NVIDIA NVENC (HEVC / AV1 / H.264), Intel QuickSync, Apple VideoToolbox',
        'Input Feeds': 'SMPTE ST 2110-20, SDI 12G/3G, NDI 6, SRT Caller/Listener, RTMP, Zixi',
        'Output Protocols': 'Low-Latency HLS (LL-HLS), MPEG-DASH, CMAF, SRT, RTMP, DVB-TS UDP',
        'Ad Insertion (SSAI)': 'SCTE-35 TS cue marker passthrough & REST ad break API triggers',
        'CDN Routing': 'Multi-CDN traffic distribution with automatic edge failover and health probe'
      }),
      features: [
        {
          title: 'Adaptive Bitrate Ladder Transcoding',
          description:
            'Generate pristine 4K UHD, 1080p60, 720p, and mobile profiles simultaneously with hardware acceleration.',
          icon: 'Cpu'
        },
        {
          title: 'SCTE-35 Digital Ad Signaling',
          description:
            'Pass through or inject SCTE-35 cue markers for server-side ad replacement across OTT and FAST platforms.',
          icon: 'Sparkles'
        },
        {
          title: 'Multi-CDN Redundancy & Edge Routing',
          description:
            'Deliver streams simultaneously to Akamai, Cloudflare, AWS CloudFront, and custom origins with automated failover.',
          icon: 'Network'
        },
        {
          title: 'Low-Latency HLS & CMAF Chunking',
          description:
            'Deliver sub-3-second glass-to-glass latency for live sports and breaking news streaming.',
          icon: 'Zap'
        }
      ],
      screenshots: [
        {
          title: 'StreamOps Transcoding & CDN Matrix',
          caption: 'Real-time multi-profile transcoding pipeline, bitrate ladder graphs, and CDN output endpoints.',
          imagePath: '/assets/products/pro-io-st2110-scte.png',
          category: 'Transcoder',
          order: 1
        },
        {
          title: 'High-Density Stream Output Engine',
          caption: 'Hardware-accelerated encoding console showing stream health, packet jitter, and audio level meters.',
          imagePath: '/assets/products/output-engine-baseband.png',
          category: 'Output Engine',
          order: 2
        }
      ]
    },
    {
      slug: 'oss-bss',
      name: 'Kashtrix OSS / BSS',
      tagline: 'Broadcast & Telecom Operations Support, Billing & Subscriber System',
      category: 'Business & Telecom',
      badge: 'OSS / BSS ENGINE',
      accentColor: '#7C3AED',
      icon: 'CreditCard',
      order: 17,
      featured: true,
      heroImage: '/assets/products/channel-controller-cluster.png',
      summary:
        'End-to-end Operations Support System (OSS) and Business Support System (BSS) for broadcast networks, cable MSOs, and OTT operators. Automates subscriber lifecycle, rate card billing, traffic invoicing, and provisioning.',
      description:
        'Kashtrix OSS / BSS unites broadcast traffic schedules, commercial ad delivery, subscriber provisioning, and financial accounting. For television broadcasters, it reconciles as-run logs with advertiser rate cards and generates automated billing invoices. For telecom and OTT providers, it manages recurring subscriber plans, payment gateway integration, and multi-tenant channel provisioning.',
      specs: JSON.stringify({
        'System Architecture': 'Microservices-based cloud/on-premise OSS/BSS with REST & GraphQL APIs',
        'Billing Modules': 'Prepaid, Postpaid, Tiered Channel Packages, Pay-Per-View, Ad Airtime Invoicing',
        'Payment Gateways': 'Stripe, PayPal, Adyen, Bank Wire Integration, Automated SEPA/ACH',
        'Traffic Reconciliation': 'Direct ingestion of Kashtrix Playout as-run logs for contract verification',
        'Security & Compliance': 'Role-based access control, PCI-DSS compliance, GDPR audit logs'
      }),
      features: [
        {
          title: 'Automated Ad Airtime Invoicing',
          description:
            'Reconcile Playout as-run transmission logs against sales contracts and auto-generate PDF invoices for commercial advertisers.',
          icon: 'FileCheck'
        },
        {
          title: 'Subscriber & Subscription Lifecycle',
          description:
            'Manage customer accounts, package entitlements, automated renewals, churn tracking, and payment processing.',
          icon: 'Users'
        },
        {
          title: 'Dynamic Rate Cards & Yield Management',
          description:
            'Set primetime, daypart, and program-specific advertising rates with automated discounts and agency commissions.',
          icon: 'TrendingUp'
        },
        {
          title: 'Automated Channel & Stream Provisioning',
          description:
            'Trigger instant playout channel activation, OTT token generation, and CDN resource allocation via REST APIs.',
          icon: 'Server'
        }
      ],
      screenshots: [
        {
          title: 'Operations & Channel Cluster Management',
          caption: 'Cluster operational dashboard displaying active channels, resource utilization, and billing status.',
          imagePath: '/assets/products/channel-controller-cluster.png',
          category: 'Operations',
          order: 1
        },
        {
          title: 'Unified Enterprise Bus & API Console',
          caption: 'Integration bus monitoring API calls between OSS/BSS billing engines and master playout controllers.',
          imagePath: '/assets/products/api-gateway-unified-bus.png',
          category: 'Integration',
          order: 2
        }
      ]
    }
  ];

  for (const prod of productsData) {
    const { features, screenshots, ...productFields } = prod;
    const createdProduct = await prisma.product.create({
      data: productFields
    });

    if (features && features.length > 0) {
      for (let i = 0; i < features.length; i++) {
        await prisma.productFeature.create({
          data: {
            productId: createdProduct.id,
            title: features[i].title,
            description: features[i].description,
            icon: features[i].icon,
            order: i + 1
          }
        });
      }
    }

    if (screenshots && screenshots.length > 0) {
      for (let i = 0; i < screenshots.length; i++) {
        await prisma.productScreenshot.create({
          data: {
            productId: createdProduct.id,
            title: screenshots[i].title,
            caption: screenshots[i].caption,
            imagePath: screenshots[i].imagePath,
            category: screenshots[i].category,
            order: screenshots[i].order || i + 1
          }
        });
      }
    }

    console.log(`✓ Seeded Product: ${createdProduct.name} (${createdProduct.slug})`);
  }

  console.log('✅ Kashtrix Database successfully seeded with 14 products, features, screenshots, and testimonials!');
}

main()
  .catch((e) => {
    console.error('Error seeding database:', e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
