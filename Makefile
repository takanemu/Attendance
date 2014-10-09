# Makefile

CC		= mcs
OPS		= -target:exe
LIBS		= -r:System.Net -r:System.Net.Http -r:packages/Newtonsoft.Json.6.0.5/lib/net45/Newtonsoft.Json -r:System.Runtime.Serialization
SRC		= MonoRaspberryPi 
OUT		= Release/bin
APP		= Attendance.exe

$(OUT)/$(APP): MonoRaspberryPi/Program.cs MonoRaspberryPi/Entities.cs MonoRaspberryPi/GpioManager.cs MonoRaspberryPi/Kintone.cs MonoRaspberryPi/FelicaReader.cs
	mkdir -p $(OUT)
	$(CC) $(OPS) -out:$(OUT)/$(APP) $(LIBS) MonoRaspberryPi/Program.cs MonoRaspberryPi/Entities.cs MonoRaspberryPi/GpioManager.cs MonoRaspberryPi/Kintone.cs MonoRaspberryPi/FelicaReader.cs
