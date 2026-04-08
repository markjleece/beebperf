# Introduction 
BeebPerf is a Windows-based profiler for the BBC Micro. It works in tandem with a modified version of BeebEm that can record execution information to a .perf file (see below).
BeebPerf can open.perf files, providing a set of interactive profiler views over the contained data.

![screenshot6jpg](https://github.com/user-attachments/assets/fb45afc5-585c-4253-bf05-db55a272828c)
![screenshot5](https://github.com/user-attachments/assets/b049e561-89bb-4dd8-b572-86adb5efa869)
![screenshot4](https://github.com/user-attachments/assets/94777952-4192-42a3-855e-79b679cd8ad7)
![screenshot3](https://github.com/user-attachments/assets/a9164ce9-d46e-424e-805f-3c995befe9a9)
![screenshot2](https://github.com/user-attachments/assets/35a829a5-1603-4ee3-8ad6-4718244e9cfc)
![screenshot1](https://github.com/user-attachments/assets/167909b8-a154-45e0-a1cc-56dbd7accab0)

# Features
-Interactive timeline, allowing selective time ranges, with zoom and ‘fit to’ features.
-Hot routines and hot path analysis.
-Multiple views including: Routines, Caller/Callee, Call Tree, Flame graph, Memory, Frames, and Code.
-Display frame reconstruction, with generated snapshots displayed under the timeline.
-Frame/game-loop analysis, allowing duration/threshold analysis and frame to display frame analysis.  The latter includes tracking screen memory writes and whether these occur before or after the display screen memory scan.
-Memory analysis, providing metrics on which memory addresses are accessed the most, and by which routines.
-Labels, which can be imported and enabled/disabled.
-Copy and Export, allowing grid data to be copied to the clipboard or exported to a CSV file.  The frame snapshots and flame graph can also be copied to the clipboard.
-Settings, allowing custom font scaling, colors, and formatting of addresses and code.  The current theme can also be selected.

# Analysis
Using the information contained within a .perf file, BeebPerf identifies and reconstructs all the stack-frames and call-stacks within the recorded program, along with all the routines.  
As part of this process BeebPerf identifies, and creates stack-frames, for all tail-calls and fall-throughs.  A tail-call is a branch or jump to another routine.  A fall-through is where execution ‘falls through’ the end of a routine to the next one.
From these the program call-tree is created, and optional IRQ/BRK and NMI call trees.
More details can be found in docs/BeebPerf.doc

# Limitations
- Some coding patterns can result in deep call paths.
- Display frame reconstruction does not currently support the hardware cursor, custom teletext modes, or custom ULA palette hardware.

**Features**
- Docter Who title music is played while the title, instructions, game complete and credits screens are shown.
- The game contains six levels, of increasing difficulty.
- Dalek samples are played if 16K of sideways RAM is available. Additional samples are played if 32K of sideways RAM is available.
- Level editor, which can be used to edit and add new levels (see below).

# Building BeebEm with performance logging feature
The performance changes add Capture Perf… and End Perf File menu items allowing a performance trace to be recorded to a .perf file.
A modified version of BeebEm can be built using Visual Studio 2022 or 2026 using the following instructions:
1.	Using the Tools  Get Tools and Features… ensure the following components are installed:
- MSVS v142 – VS 2019 C++ x64/x86 build tools (v14.29 – 16.11)
- C++ v14.29 (16.11) MFC for v142 build tool (x86 & x64)
 – needed for afxres.h/rc

2.	Using the Git  Clone Repository…, clone the BeebEm repository: ‘https://github.com/stardot/beebem-windows.git’
3.	Using the Git  Open in Command Prompt, execute the following git command to apply the performance changes.  The path to the patch file will likely need changing.
   git apply ..\beebperf\beebem_changelist.diff
4.	Reopen and build the solution
5.	Run BeebEm and verify that the File menu contains a Capture Perf… and End Perf menu items.

## Other files
| File name | Description |
|---|---|
| docs\BeebPerf.docx | Provides details on BeebPerf, which may be of interest |

# Credits
Portions of the display reconstruction code were copied from BeebEm's video class, specifically the Mode 7 font, font loading code, and state machine code.

# Copyright
Copyright (C) 2026 Mark John Leece

# License
'BeebPerf' is distributed under the terms of the GNU General Public License as described in [COPYRIGHT.txt](COPYRIGHT.txt)
