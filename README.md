# TodoTxtDaemon

This is a tiny daemon that monitors your [todo.txt](https://github.com/todotxt/todo.txt) 
file and moves completed tasks to your done.txt file. 

## Features

* Moves tasks once per day at 03:00.
* Moves tasks as soon as possible when the computer was powered off or hiberated for a longer period.
* Automatically prepends the task completion date based on the last write time of `todo.txt`. 
* If the last write time is between 00:00 and 03:00, the task is considered to be completed on the previous day.
* Low CPU and memory footprint.

## Installation

Download the [latest release](https://github.com/yavorfingarov/TodoTxtDaemon/releases) for your OS and 
extract it somewhere on your hard drive. If you don't have the .NET 7 runtime installed, choose 
the `self-contained` build.

## Usage

Edit `appsettings.ini` and add the paths of your `todo.txt` and `done.txt` files.

Set up the daemon to start at log on:

### Windows

* Open _Administrative Tools > Task Scheduler_
* In the _Actions_ panel, click _Create Basic Task_
* Enter a name and click _Next_
* Select _When I log on_ and click _Next_
* Select _Start a program_ and click _Next_
* In the _Program/script_ field, locate the `TodoTxtDaemon.exe` and click _Next_
* Select _Open the Properties dialog..._ and click _Finish_
* Select _Run whether user is logged on or not_ and select _Do not store password..._
* In the _Conditions_ tab, unselect _Start the task only if the computer is on AC power_
* In the _Settings_ tab, unselect _Stop the task if it runs longer..._

### MacOS

* Open _System Preferences > Users & Groups_
* In the _Login Items_ tab, click on the lock icon and enter your admin password
* Click _+_ and locate the `TodoTxtDaemon` executable

### Linux

* _(GNOME, Cinnamon, MATE, Unity)_ Open _Startup Applications Preferences_
* _(KDE)_ Open _System Settings > Startup and Shutdown_ and select the _Autostart_ panel
* _(Xfce)_ Open _Settings Manager > Session and Startup_ and select the _Application Autostart_ tab
* Add the `TodoTxtDaemon` executable

Once the daemon is running, check `app.log` if everything works as expected.

## Support

If you spot any problems and/or have improvement ideas, please share them via
the [issue tracker](https://github.com/yavorfingarov/TodoTxtDaemon/issues).
