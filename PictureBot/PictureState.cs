using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureBot
{
    public class PictureState
    {

        /// <summary>
        /// Gets or sets the number of turns in the conversation.
        /// </summary>
        /// <value>The number of turns in the conversation.</value>
        public string Greeted { get; set; } = "not greeted";
        public List<string> UtteranceList { get; private set; } = new List<string>();

        public string Search { get; set; } = "";
        public string Searching { get; set; } = "no";
    }
}
