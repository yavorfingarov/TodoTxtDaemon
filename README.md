# TodoTxtDaemon

This is a tiny daemon that monitors your [`todo.txt`](https://github.com/todotxt/todo.txt) file and moves completed tasks to your `done.txt` file. 

## Features

* Moves tasks once per day at 03:00.
* Moves tasks as soon as possible when the computer was powered off or hiberated for a longer period.
* Automatically prepends the task completion date based on the last write time of `todo.txt`. 
* If the last write time is between 00:00 and 03:00, the task is considered to be completed on the previous day.
* Low CPU and memory footprint.

## Installation

Download the latest release for your OS and extract it somewhere on your hard drive. If you don't have the .NET 6 runtime installed, choose the `self-contained` version.

## Usage

Edit `appsettings.json` and add the paths of your `todo.txt` and `done.txt` files.

Set up the daemon to start at log on. Make sure it will run in the directory containing the executable.

* [Windows instructions](https://www.wintips.org/how-to-start-a-program-at-startup-with-task-scheduler/)
* [MacOS instructions](https://www.idownloadblog.com/2015/03/24/apps-launch-system-startup-mac/)
* [Linux instructions](https://www.xmodulo.com/start-program-automatically-linux-desktop.html)

Once the daemon is running, check `app.log` if everything works as expected.
