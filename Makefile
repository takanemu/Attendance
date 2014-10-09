# Makefile

CC		= mcs

Program.exe: MonoRaspberryPi/Program.cs
	mkdir -p Release/bin
	$(CC) -target:exe -out:Release/bin/Attendance.exe -r:System.Net -r:System.Net.Http -r:packages/Newtonsoft.Json.6.0.5/lib/net45/Newtonsoft.Json -r:System.Runtime.Serialization MonoRaspberryPi/Program.cs

