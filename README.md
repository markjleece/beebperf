# BeebPerf - Windows based Profiler for the BBC Micro
**BeebPerf** is a Windows-based profiler for the **BBC Micro** that provides a set of interactive profiler views on a performance session (**.perf** file) recorded using a modified version of **BeebEm**.

<img width="300" alt="Call Tree" src="https://github.com/user-attachments/assets/79e91976-3bd3-4232-ba3d-3b7f7300841c" />
<img width="300" alt="Flame Graph" src="https://github.com/user-attachments/assets/c5395d71-5af1-405a-8cdf-2482860ca6a1" />
<img width="300" alt="Metric" src="https://github.com/user-attachments/assets/f9f5eb12-8ec4-46ea-9885-f13f64a7738f" />
<img width="300" alt="Memory" src="https://github.com/user-attachments/assets/4b534966-8a6a-431c-bef9-da541a2efded" />
<img width="300" alt="Routines" src="https://github.com/user-attachments/assets/a8651ef9-b50f-45af-b89c-d72ed421c4b5" />
<img width="300" alt="Dark Mode" src="https://github.com/user-attachments/assets/840dd5a8-18c3-4795-8a34-971900b5db0f" />

# Features
- **Hot routines** and **hot path** analysis.
- **Game-loop analysis**, providing metrics on durations, and potential display frame misalignment and tearing.  The analysis tracks screen memory writes and whether these occur before or after the screen memory is scanned for display.
- **Memory analysis**, providing metrics on which memory addresses are accessed the most, by which routines, and by which instructions.
- **Interactive timeline and profiler views** including: Timeline, Call Tree, Flame Graph, Routines, Caller/Callee, Memory, Metrics, and Code.
- **Display frame reconstruction**, with frames  displayed under the timeline.
- **Label support** - labels can be imported from assembler output files, or optionaly embedded in **.perf** files (see below).
- **Copy and Export** - grid data to be copied to the clipboard or exported to a CSV file.  Display frames and Flame Graph images can also be copied to the clipboard.

# Building and running BeebPerf
**BeebPerf** can be built using **Visual Studio 2022** or **2026**.  The following Visual Studio workloads and components are required:

Workloads:
   - .NET desktop development
   - Desktop development with C++

Individual components:
   - MSVC Build Tools for x64/x86 (Latest)
   
After cloning this repository, open the solution file: **BeebPerf.sln** and **Rebuild All**. If you are using **Visual Studio 2022**, you will need to update the **Zlib** project’s **Platform Toolset** to **v143**.

Then open one of the sample **.perf** files from the **sample** folder (e.g. **elite.perf** or **revs.perf**) to test the build and click the **Help** toolbar button to become familiar with the profiler's functionality.
 
# Limitations
1.	**Label duplication**.  If multiple labels map to the same address, the incorrect label may be displayed.
2.	**Overlapping code**.  If code is loaded and executed over previously executed code, the profiler may not function correctly.  Current algorithms for identifying routines and stack frames does not support overlapping code.
3.	**Code-patterns**.  Some 6502 coding patterns can result in deep tail-call paths being generated, which can impede analysis.  For example, calling into the middle of a loop from  another routine can result in deep call-paths.
4.	**Display-frame reconstruction**. Does not support custom teletext modes or custom ULA hardware.

# Documentation
| File name | Description |
|---|---|
| [docs/Help.docx](docs/Help.docx) | Getting started guide, which is also displayed by clicking on the **Help** toolbar button |
| [docs/BeebPerf.docx](docs/BeebPerf.docx) | Details on the development of **BeebPerf**, which may be of interest |
| [docs/FileFormat.docx](docs/FileFormat.docx) | **.Perf** file format specification (v1.0) |

# Building BeebEm with performance logging capabilities
The performance changes add **Capture Perf…** and **End Perf** File menu items allowing a performance session to be recorded to a **.perf** file.
A modified version of **BeebEm** can be built using **Visual Studio 2022** or **2026** using the following instructions:
1.	Using the **Tools** -> **Get Tools and Features…** ensure the following workloads and individual components are installed:

    Workloads:
    - Desktop development with C++

    Individual components:
    - MSVC Build Tools for x64/x86 (Latest)
    - C++ MFC for x64/x86 (Latest MSVC) – needed for afxres.h/rc
      
2.	Download and install the [Microsoft DirectX SDK (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=6812) from the Microsoft download site. See **Known Issues** below if the installation fails.

4.	Using the **Git** -> **Clone Repository…** menu item, clone the **BeebEm** repository: [beebem-windows.git](https://github.com/stardot/beebem-windows.git)
5.	Open the **BeebEm** solution and using  **Git** -> **Open in Command Prompt** menu item, execute the following **git** command to apply the performance changes: **<code style="background:#333; color:#ffcc00; padding:4px 6px; border-radius:4px; font-family:'Fira Code', monospace;">git apply ..\\beebperf\\beebem_changelist.diff</code>**  (the path to the patch file will likely need changing).
6. Reopen and rebuild the solution (x64 configuration). You may need to individually reload each of the solution’s projects before building.  This can be achieved by right-clicking on each project and selecting **Reload Project**. If you are using **Visual Studio 2022**, you will need to update all the projects **Platform Toolsets** to **v143**. 
7.	Run **BeebEm** and verify that the **File** menu contains a **Capture Perf…** and **End Perf** menu items. You may need to set the **BeebEm** project as the **Startup Project**.  This can be achieved by right-clicking on the **BeebEm** project and selecting **Set as Startup Project**.

**Known Issues**

- **DirectX SDK**

   If the **DirectX SDK** installer reports error S1023, the most likely cause is that newer Microsoft Visual C++ 2010 redistributables are installed on the system.
   The fix is to temporarily uninstall the newer VC++ 2010 x86/x64 redistributables, and then reinstall the **DirectX SDK**.

- **Protected folder access blocked**

   If on **Windows 11** you see a ‘Protected folder access blocked’ notification after closing **BeebEm**, or when saving **BeebEm** preferences, these are expected as **Windows Defender’s CFA** (Controller Folder Access) blocks writes to the documents folder, which is where **BeebEm** stores its preferences. 
   The fix is to add **BeebEm.exe** (debug and release versions) to the allowed app list under **Controlled folder access** in **Windows Settings**.

The changes to **BeebEm** are kept to a minimum, limited to just those needed to generate **.perf** files.

See [docs/BeebPerf.docx](docs/BeebPerf.docx) for more information.

# Acknowlegements
- Portions of the display reconstruction code were based on **BeebEm's** video class. Many thanks to the **BeebEm** contributors for creating and maintaining this wonderful piece of software. 
- The **.perf** files are compressed and uncompressed using the **ZLib library** - Many thanks to **Jean-loup Gailly** and **Mark Adler** for creating such an excellent and useful library.
- The MOS 1.2 and 2.0 labels were taken from Toby Nelson’s **MOS Reassembly for the BBC Micro**. Thank you Toby for creating and maintaining this excellent resource.

# BeebPerf Copyright
Copyright (C) 2026 Mark John Leece

# License
'BeebPerf' is distributed under the terms of the GNU General Public License as described in [COPYRIGHT.txt](COPYRIGHT.txt)
