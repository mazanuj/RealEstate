using RealEstate.Log;
using System.Linq;

namespace RealEstate.Commands
{
    public class LogModule : Module
    {
        private readonly LogManager _logManager;

        public LogModule(LogManager logManager)
        {
            _logManager = logManager;
        }

        public override bool Process(string[] args)
        {
            if (base.Process(args))
                return true;
            var count = args.Count();

            if (count == 2 && args[1] == "clear")
            {
                _logManager.ClearLogFile();
            }
            else
            {
                Write("Proper command not found!");
            }

            return true;
        }
        public override string Description
        {
            get { return "Manage log file"; }
        }

        public override string Help
        {
            get { return "log \r\n\t\t[clear]"; }
        }
    }

}
