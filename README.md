# BeebPerf - Windows based Profiler for the BBC Micro
**BeebPerf** is a Windows-based profiler for the **BBC Micro** that provides a set of interactive profiler views on a performance session (**.perf** file) recorded using a modified version of **BeebEm**.

<img width="300" alt="layout" src="https://github.com/user-attachments/assets/167909b8-a154-45e0-a1cc-56dbd7accab0" />
<img width="300" alt="flame graph" src="https://github.com/user-attachments/assets/35a829a5-1603-4ee3-8ad6-4718244e9cfc" />
<img width="300" alt="memory" src="https://github.com/user-attachments/assets/a9164ce9-d46e-424e-805f-3c995befe9a9" />
<img width="300" alt="frames" src="https://github.com/user-attachments/assets/94777952-4192-42a3-855e-79b679cd8ad7" />
<img width="300" alt="call tree" src="https://github.com/user-attachments/assets/b049e561-89bb-4dd8-b572-86adb5efa869" />
<img width="300" alt="caller / callee" src="https://github.com/user-attachments/assets/fb45afc5-585c-4253-bf05-db55a272828c" />

# Features
- **Hot routines** and **hot path** analysis.
- **Frame/game-loop analysis**, allowing duration/threshold analysis and frame to display-frame analysis.  The latter includes tracking screen memory writes and whether these occur before or after the screen memory is scanned for display.
- **Memory analysis**, providing metrics on which memory addresses are accessed the most, by which routines, and by which instructions.
- **Interactive timeline and profiler views** including: Timeline, Routines, Caller/Callee, Call Tree, Flame graph, Memory, Frames, and Code.
- **Display frame reconstruction**, with generated snapshots displayed under the timeline.
- **Label support** - labels can be imported from assembler output files, or optionaly embedded in **.perf** files (see below).
- **Copy and Export** - grid data to be copied to the clipboard or exported to a CSV file.  Frame snapshots and flame graph images can also be copied to the clipboard.

# Building and running BeebPerf
**BeebPerf** can be built using **Visual Studio 2022** or **2026**.  The following Visual Studio components are required:
   - .Net 10 runtime and development tools
   - MSVS v142 – VS 2019 C++ x64/x86 build tools (v14.29 – 16.11)

After cloning this repository, open the solution file: **BeebPerf.sln**, rebuild all, set the **BeebPerf** project as the startup project, and run it.

Then open one of the sample **.perf** files from the **sample** folder (e.g. **elite.perf** or **revs.perf**) to test the build and become familiar with the profiler's functionality.  A **'getting started'** guide can be shown by clicking the toolbar **help** button.  
 
# Limitations
1.	**Label duplication**.  If multiple labels map to the same address, the incorrect label may be displayed.
2.	**Overlapping code**.  If code is loaded and executed over previously executed code, the profiler may not function correctly.  Current algorithms for identifying routines and stack frames does not support overlapping code.
3.	**Code-patterns**.  Some 6502 coding patterns can result in deep tail-call paths being generated, which can impede analysis.  For example, calling into the middle of a loop from  another routine can result in deep call-paths.
4.	**Display-frame reconstruction**. Does not support custom teletext modes or custom ULA hardware.

# Documentation
| File name | Description |
|---|---|
| [docs/GettingStartedGuide.docx](docs/GettingStartedGuide.docx) | Getting started guide |
| [docs/BeebPerf.docx](docs/BeebPerf.docx) | Details on the development of **BeebPerf**, which may be of interest |
| [docs/FileFormat.docx](docs/FileFormat.docx) | **.Pref** file format specification (v1.0) |

# Building BeebEm with performance logging capabilities
The performance changes add **Capture Perf…** and **End Perf** File menu items allowing a performance session to be recorded to a **.perf** file.
A modified version of **BeebEm** can be built using **Visual Studio 2022** or **2026** using the following instructions:
1.	Using the **Tools** -> **Get Tools and Features…** ensure the following components are installed:
   - MSVS v142 – VS 2019 C++ x64/x86 build tools (v14.29 – 16.11)
   - C++ v14.29 (16.11) MFC for v142 build tool (x86 & x64) – needed for afxres.h/rc
2.	Download and install the [Microsoft DirectX SDK (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=6812) from the Microsoft download site.
3.	Using the **Git** -> **Clone Repository…** menu item, clone the BeebEm repository: [beebem-windows.git](https://github.com/stardot/beebem-windows.git)
4.	Using  **Git** -> **Open in Command Prompt** menu item, execute the following **git** command to apply the performance changes: **<code style="background:#333; color:#ffcc00; padding:4px 6px; border-radius:4px; font-family:'Fira Code', monospace;">git apply ..\\beebperf\\beebem_changelist.diff</code>**  (the path to the patch file will likely need changing).
5. Reopen and rebuild the solution (x64 configuration). You may need to individually reload each of the solution’s projects before building.  This can be achieved by right-clicking on each project and selecting **‘Reload’**.
6.	Run **BeebEm** and verify that the **File** menu contains a **Capture Perf…** and **End Perf** menu items. You may need to set the **BeebEm** project as the Startup Project.  This can be achieved by right-clicking on the **BeebEm** project and selecting **‘Set as Startup Project’**.

The changes to **BeebEm** are kept to a minimum, limited to just those needed to generate **.perf** files.

See [docs/BeebPerf.docx](docs/BeebPerf.docx) for more information.

# Acknowlegements
- Portions of the display reconstruction code were based on **BeebEm's** video class. Many thanks to the **BeebEm** contributors for creating and maintaining this wonderful piece of software. 
- The **.perf** files are compressed and uncompressed using the **ZLib library** - Many thanks to **Jean-loup Gailly** and **Mark Adler** for creating such an excellent and useful library. 

# BeebPerf Copyright
Copyright (C) 2026 Mark John Leece

# License
'BeebPerf' is distributed under the terms of the GNU General Public License as described in [COPYRIGHT.txt](COPYRIGHT.txt)
