using System;
using System.IO;
using System.Management.Automation;

namespace pacfiles
{
    [Cmdlet(VerbsCommon.Find,"ProxyForURL",
        DefaultParameterSetName = "PacFile")]
    [OutputType(typeof(string))]
    public class FindProxyForURLCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            ParameterSetName = "PacFile",
            Position = 0)]
        [Alias("File", "Pac")] 
        public string PacFile { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = "PacFunction",
            Position = 0)]
        [Alias("Function")]
        public string PacFunction { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1)]
        public Uri Url { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            Position = 1)]
        new public string Host { get; set; } = null;

        pacparser pacparser;

        protected override void BeginProcessing()
        {
            if (ParameterSetName == "PacFile")
            {
                PacFunction = File.ReadAllText(PacFile);
            }
            pacparser = new pacparser(PacFunction);
        }

        protected override void ProcessRecord()
        {
            WriteObject(pacparser.FindProxyForURL(Url, Host));
        }
    }
}
