# 🍒 Lumina Music - Self-Host Music Player

![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)
![Framework](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

**Lumina Music** is an open-source online music streaming application featuring a modern Cherry Dark aesthetic. It supports synced lyrics, YouTube audio streaming/casting, and **Lumina Connect** for real-time remote music control across multiple devices.

---

## 🚀 Key Features

- 🎵 **Zero-Delay Seeking:** Integrated Howler.js with the Web Audio API for instant seeking without audio buffering or latency.
- 📜 **Synced Lyrics:** Automatically fetches and smoothly scrolls lyrics in real time using native JavaScript algorithms.
- 📻 **YouTube Audio Stream & Cast:** Decodes and streams audio directly from YouTube URLs or casts audio playback to PC speakers/smart devices.
- 📲 **Lumina Connect (SignalR Hub):** Real-time multi-device synchronization for Play/Pause, seeking, track switching, and volume adjustment.
- 🎨 **Cherry Aesthetic UI:** Features falling cherry blossom animations, glassmorphism UI, Fullscreen Player, and spinning vinyl disc visuals.
- 🌐 **Multi-Language Support:** Fully supports 3 languages: Vietnamese (VN), English (EN), and Japanese (JA).

---

## 🛠️ Tech Stack

### **Backend**
- **Framework:** ASP.NET Core Web API / C#
- **Real-time Communication:** SignalR Hub (`LuminaConnectHub`)
- **Third-party Integration:** `yt-dlp` (YouTube stream decoding), LRCLIB API (Synced lyrics fetching)

### **Frontend**
- **Core:** HTML5, CSS3 (CSS Variables, Animations, Glassmorphism), JavaScript (ES6+)
- **Audio Engine:** Howler.js (Web Audio API & HTML5 Audio Fallback)
- **Libraries:** jQuery 3.6.0, FontAwesome 6.4.0

---

## 📦 Getting Started

### **Prerequisites**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or higher
- Python 3.x & `yt-dlp` (Required for YouTube Stream/Cast functionality)

### **Installation & Running**

1. **Clone the repository:**
   ```bash
   git clone https://github.com/LuxiousTeam/Lumina-Music.git
   cd Lumina-Music
   ```

2. Run the Backend & Web Client:

```Bash
cd Lumina.API
dotnet run
```

3. Access the application:
Open your browser and navigate to:

```Plaintext
http://localhost:5067
```

🤝 Contributing
Developed and maintained by Luxious Team. Contributions, issues, and feature requests are welcome!

📄 License
Distributed under the MIT License. See LICENSE for more information.

Copyright (c) 2026 Nam (Luxious Team). All rights reserved.