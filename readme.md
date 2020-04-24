# CoreBoy

A .NET Core Gameboy emulator that started life as a port of Coffee-GB (https://github.com/trekawek/coffee-gb).
MIT licensed, go nuts.

# Docs

This- 

* Runs Gameboy and Gameboy Color games.
* Has a headless CLI mode
* Has a Windows-Only WinForms UI
* Can be used as a library in your own software

# Pre-Reqs

* .NET Core 3.1

# Usage

## Windows

Just run `CoreBoy.Windows` and load a ROM from the file menu!

## Mac / Linux

Command line:

Just run `CoreBoy.Avalonia` and load a ROM from the file menu!

# Controls

	LeftArrow = Left
	RightArrow = Right
	UpArrow = Up
	DownArrow = Down
	Z = A
	X = B
	Enter = Start
	Backspace = Select

# Audio

Isn't working yet.

# Resizing

Is currently buggy and slow, because it's just "whatever WinForms is doing" rather than explicitly scaled rendering.
I'll get around to it.
