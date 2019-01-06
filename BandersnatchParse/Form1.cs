using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace BandersnatchParse
{
    public partial class Form1 : Form
    {
        JObject props;
        int indentLevel = 0;
        String nl = Environment.NewLine;
        string lastCondition = "none";

        public Form1()
        {
            InitializeComponent();
        }

        private void Log(string text)
        {
            if (text != null)
            {
                var curLine = textBox1.GetLineFromCharIndex(this.textBox1.SelectionStart);
                string curLineText = "";
                if (textBox1.Lines.Count() > 0)
                    curLineText = textBox1.Lines[curLine];

                if (curLineText == "")
                {
                    var indent = "".PadLeft(indentLevel * 2);
                    textBox1.AppendText(indent + text);
                } else
                {
                    textBox1.AppendText(text);
                }
                
            }
        }

        void Sort(JObject jObj)
        {
            var props = jObj.Properties().ToList();
            foreach (var prop in props)
            {
                prop.Remove();
            }

            foreach (var prop in props.OrderBy(p => p.Name))
            {
                jObj.Add(prop);
                if (prop.Value is JObject)
                    Sort((JObject)prop.Value);
            }
        }

        private JToken GetChoices(string segmentId)
        {
            Log("ch(" + segmentId + ") > ");

            var moment = props.SelectToken("interactiveVideoMoments.momentsBySegment['" + segmentId + "'][?(@.type == 'scene:cs_bs')]");

            if (moment == null) //Not found in moments list, check if actually segmentGroup
            {
                moment = props.SelectToken("interactiveVideoMoments.segmentGroups['" + segmentId + "']");
                if (moment != null) //Is actually a segmentGroup
                {
                    EvalSegmentGroup(segmentId);
                } else
                {
                    Log("END");
                    return null;
                }
            }

            var choices = moment.SelectToken("$.choices");

            return choices;
        }

        private void EvalChoices(JToken choices)
        {
            if (choices == null)
                return;

            foreach (JToken choice in choices.Children())
            {
                string segmentId = (string)choice.SelectToken("$.segmentId");
                string sg = (string)choice.SelectToken("$.sg");

                if (segmentId != null)
                    EvalChoices(GetChoices(segmentId));

                if (sg != null)
                    EvalSegmentGroup(sg);

                Log("\n");
            }
        }

        private void EvalSegmentGroup(string sg)
        {
            Log("sg(" + sg + ") > ");
            var segments = props.SelectToken("interactiveVideoMoments.segmentGroups['" + sg + "']");

            foreach (var segment in segments.Children())
            {
                if (segment.SelectToken("segmentGroup") != null)
                {
                    EvalSegmentGroup((string)segment.SelectToken("segmentGroup"));
                } else if (segment.SelectToken("segment") != null) {
                    foreach (string subSegment in segment.SelectToken("segment"))
                    {
                        EvalChoices(subSegment);
                    }
                } else
                {
                    EvalChoices(GetChoices((string)segment));
                }
                
            }
        }

        private void UnwrapPreconditions(JToken preconditions, int depth = 2)
        {
            if (preconditions != null)
            {            
                foreach (var precondition in preconditions.Children())
                {
                    indentLevel = depth;
                    if (precondition.Count() > 1)
                    {
                        Log(":" + nl);
                        UnwrapPreconditions(precondition, depth+1);
                    }
                    else
                    {
                        if (precondition.ToString() != "persistentState")
                        {
                            lastCondition = precondition.ToString();
                            Log(precondition.ToString() + " " + precondition.Parent.Parent.First.ToString());
                        }
                    }
                }
            }
        }

        private void ProcessMoments()
        {
            var moments = props.SelectToken("interactiveVideoMoments.momentsBySegment");
            Sort((JObject)moments);

            foreach (JProperty moment in moments.Children())
            {
                indentLevel = 0;
                Log(moment.Name + " ");

                var description = (string)props.SelectToken("interactiveVideoMoments.choicePointNavigatorMetadata.choicePointsMetadata.choicePoints." + moment.Name + ".description");

                if (description != null)
                {
                    Log("\"" + description + "\" ");
                }

                Log("{ " + nl);

                indentLevel = 1;

                var persistentData = moment.Value.SelectToken("$.[?(@.type == 'scene:cs_bs')].impressionData.data.persistent");
                if (persistentData != null)
                    Log(persistentData.ToString().Trim(new Char[] { '{', '}' }).TrimStart().TrimEnd().Replace("\"", "") + nl + nl);

                persistentData = moment.Value.SelectToken("$.[?(@.type == 'notification:playbackImpression')].impressionData.data.persistent");
                if (persistentData != null)
                    Log(persistentData.ToString().Trim(new Char[] { '{', '}' }).TrimStart().TrimEnd().Replace("\"", "") + nl + nl);

                var preconditions = moment.Value.SelectToken("$.[?(@.type == 'notification:playbackImpression')].precondition");

                if (preconditions != null)
                {
                    Log("req:" + nl);
                    UnwrapPreconditions(preconditions);
                    Log(nl + nl);
                }

                indentLevel = 1;

                var choices = moment.Value.SelectTokens("$.[?(@.type == 'scene:cs_bs')].choices");

                if (choices != null)
                {
                    foreach (var choice in choices.Children())
                    {
                        if (choice["segmentId"] != null)
                            Log((string)choice["text"] + ": " + (string)choice["segmentId"] + (string)choice["sg"] + nl);
                        if (choice["sg"] != null)
                            Log((string)choice["text"] + ": /" + (string)choice["sg"] + "/" + nl);
                    }
                }

                indentLevel = 0;

                Log(nl + "} " + nl + nl);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TextReader tr = new StreamReader(@"Bandersnatch.json");
            string jsonString = tr.ReadToEnd();

            var bandersnatch = new Bandersnatch();

            props = JObject.Parse(jsonString);

            Log("Going to beginning of movie...\n");

            //EvalChoices(GetChoices("1A"));
            //ProcessMoments();

            var moments = props.SelectToken("interactiveVideoMoments.preconditions");
            foreach (JProperty preconditions in moments.Children())
            {
                indentLevel = 0;
                Log(preconditions.Name + " ");
                UnwrapPreconditions(preconditions);
                Log(nl + nl);
            }

        }
    }

    class BChoice
    {
    }
}
