using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Fiddler;

namespace SmileVideoExporter
{
    [ProfferFormat("SmileVideo", "Saves video from SmileVideo with friendly FileName.")]
    public class SmileVideoExporter: ISessionExporter
    {
        static Regex pathMatcher = new Regex(@"^(.*)sm\d+(.*?)\.(?:mp4|flv)$");
        static Regex uriMatcher = new Regex(@"smile-\w+\.nicovideo\.jp/smile\?.=(\d+)");
        #region ISessionExporter メンバー

        public bool ExportSessions(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications)
        {
            if (sExportFormat != "SmileVideo") return false;

            string sampleFilename = null;
            string[] filenameBase = new string[]{null};

            if (dictOptions != null && dictOptions.ContainsKey("FileName"))
                sampleFilename = dictOptions["FileName"] as string;
            if(sampleFilename != null)
                filenameBase = pathMatcher.Match(sampleFilename).GetGroupValues().ToArray();

            if (string.IsNullOrEmpty(filenameBase[0]))
            {
                sampleFilename = Utilities.ObtainSaveFilename("Export as SmileVideo", "Video (use sm{num} as placeholder)|*.mp4;*.flv");
                if (sampleFilename != null)
                    filenameBase = pathMatcher.Match(sampleFilename).GetGroupValues().ToArray();
                if (string.IsNullOrEmpty(filenameBase[0]))
                    return false;
            }

            bool isHitted = false;
            int processedCount = 0;

            foreach (var s in oSessions)
            {
                var vid = uriMatcher.Match(s.url).Groups[1].Value;
                var ctype = s["Response","Content-Type"];
                if (!string.IsNullOrEmpty(vid) && ctype.StartsWith("video/"))
                {
                    var filename = string.Format("{0}sm{1}{2}.{3}", filenameBase[1], vid, filenameBase[2], ctype.Substring(6));
                    if (evtProgressNotifications != null)
                    {
                        var args = new ProgressCallbackEventArgs((float)processedCount++ / oSessions.Length, string.Format("Saving sm{0}...", vid));
                        evtProgressNotifications(this, args);
                        if (args.Cancel) return false;
                    }
                    s.SaveResponseBody(filename);
                    isHitted = true;
                }
            }

            return isHitted;
        }

        #endregion

        #region IDisposable メンバー

        public void Dispose()
        {
        }

        #endregion
    }

    static class MatchExtentions
    {
        public static IEnumerable<string> GetGroupValues(this Match m)
        {
            return m.Groups.Cast<Group>().Select(g => g.Value);
        }
    }
}
