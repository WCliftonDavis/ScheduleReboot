// This utility is used to reboot a machine at a specific time, solving the problem of implementing the reboot after it wakes up if it is in sleep mode during the target time.
// The reboot time is identified on the initial run however the reboot isn't scheduled until five minutes before that time.
// If the machine is in sleep mode when the reboot time arrives, the process will be restarted upon resume and the utility will identify that the target time is in the past.
// The reboot will be immediatly scheduled for 300 seconds, five minutes, from the time the process resumes.
// Usage of the tool is described below and can be accessed by issuing the ScheduleReboot with incomplete parameters.
using System;
using static System.Console;
using System.Diagnostics;
using System.IO;


namespace ScheduleReboot
{
    class Program
    {
        static void UtilityInstructions()
        {
            Console.WriteLine("This utility initiates a reboot at the specified time.\n");
            Console.WriteLine("First argument: \tDay of week to reboot, expressed as an integer with Sunday as day 1.");
            Console.WriteLine("Second argument: \tNumber 0 through 23 representing the hour to reboot.");
            Console.WriteLine("Third argument: \tNumber 0 through 59 representing the minute to reboot.");
            Console.WriteLine("Fourth argument: \t-r or /r to reboot.  -s or /s to shutdown.");
            Console.WriteLine("Fifth argument: \tNumber of seconds of uptime required before scheduling a reboot.\n");
            Console.WriteLine("Examples:");
            Console.WriteLine("ScheduleReboot 7 2 0 -r 86400\tReboots on Sunday at 0200 if a reboot has not occured within one day.");
            Console.WriteLine("ScheduleReboot 6 23 0 -s 259200\tShuts down on Saturday at 2300 if a reboot has not occured within three days.\n");

        }
        static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                UtilityInstructions();
            }
            else
            {
                System.IO.Directory.CreateDirectory(@"C:\Logs\TimedShutdown");

                // Identify whether a valid reboot/shutdown option has been provided in the fourth argument.
                if (((args[3] == "-r") || (args[3] == "/r") || (args[3] == "-s") || (args[3] == "-s")) != true)
                {
                    UtilityInstructions();
                    Console.WriteLine("The fourth argument must be a reboot option of -r or /r or a shutdown option of -s or /s.");
                    System.Environment.Exit(1);
                }

                // Identify the reboot day.
                int rebootDay;
                if (((Int32.TryParse(args[0], out rebootDay)) == false) || (rebootDay > 7) || (rebootDay < 1))
                {
                    UtilityInstructions();
                    Console.WriteLine("The day of the week argument was not an integer between 1 and 7.");
                    System.Environment.Exit(1);

                }

                // Identify the reboot hour.
                int rebootHour;
                if (((Int32.TryParse(args[1], out rebootHour)) == false) || (rebootHour > 23) || (rebootHour < 0))
                {
                    UtilityInstructions();
                    Console.WriteLine("The hour was not an integer between 0 and 23.");
                    System.Environment.Exit(1);

                }

                // Identify the reboot minute.
                int rebootMinute;
                if (((Int32.TryParse(args[2], out rebootMinute)) == false) || (rebootMinute > 59) || (rebootHour < 0))
                {
                    UtilityInstructions();
                    Console.WriteLine("The minute was not an integer between 0 and 59.");
                    System.Environment.Exit(1);

                }

                int uptimeSecondsAllowed;
                if (((Int32.TryParse(args[4], out uptimeSecondsAllowed)) == false) || (uptimeSecondsAllowed < -1))
                {
                    UtilityInstructions();
                    Console.WriteLine("The uptime expected was not a positive integer.");
                    System.Environment.Exit(1);

                }
                // Wait here until the uptime has exceeded the number of seconds of uptime expected before a reboot it initiated.
                while ((Stopwatch.GetTimestamp() / Stopwatch.Frequency) < uptimeSecondsAllowed)
                {
                    // Put the thread to sleep for 60 seconds to avoid CPU consuming cycles.
                    System.Threading.Thread.Sleep(60000);
                }

                DateTime currentDate = DateTime.Now;

                // Identifies the next iteration of the target day subtracting one to account for the current day.  Add seven days if the day is in the following week.
                int daysToNextRebootTime = (rebootDay - ((int)currentDate.DayOfWeek) - 1);
                if (daysToNextRebootTime < 1)
                {
                    daysToNextRebootTime = (daysToNextRebootTime + 7);
                }

                // Calculate the seconds from current time until the reboot time.  Subtract 300 seconds to allow a five minute user warning.
                int hoursToNextRebootTime = (rebootHour - currentDate.Hour);
                int minutesToNextRebootTime = (rebootMinute - currentDate.Minute);
                int secondsToNextRebootTime = (00 - currentDate.Second);
                int secondsUntilReboot = (((daysToNextRebootTime * 86400) + (hoursToNextRebootTime * 3600) + (minutesToNextRebootTime * 60) + secondsToNextRebootTime) - 300);
                DateTime dateForReboot = DateTime.Now.AddSeconds(secondsUntilReboot);

                // Write the target time to a local log.
                try
                {
                    string logLocation = (@"C:\Logs\TimedShutdown\" + DateTime.Now.Year + DateTime.Now.Month.ToString("d2") + DateTime.Now.Day.ToString("d2") + ".txt");
                    using (StreamWriter fileToWriteTo = System.IO.File.AppendText(logLocation))
                    {
                        fileToWriteTo.WriteLine("\n{0}", DateTime.Now);
                        fileToWriteTo.WriteLine("Five minute user warning will be issued at: {0}", dateForReboot);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to create or access log directory for ScheduledReboot.exe utility.");
                }

                while (true)
                {
                    // Test for a negative time condition to occur indicating that the target time has passed.
                    TimeSpan deltaOfSecondsForReboot = (dateForReboot - DateTime.Now);
                    if (deltaOfSecondsForReboot.TotalSeconds < 0)
                    {
                        string shutdownArguments = args[3] + " /t 300";
                        Process.Start("shutdown", shutdownArguments);
                    }
                    // Put the thread to sleep for 60 seconds to avoid CPU consuming cycles.
                    System.Threading.Thread.Sleep(60000);
                }
            }
        }
    }
}
