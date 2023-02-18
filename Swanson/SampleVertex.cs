using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swanson
{
    class SampleVertex : INotifyPropertyChanged
    {
        private bool active;
        private bool visited;
        private string text;
        private string color = "Black";
        private string adjecenct;
        public List<SampleVertex> adjacencyList;
        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                NotifyChanged("Active");
            }
        }
        public bool Visited
        {
            get { return visited; }
            set
            {
                visited = value;
                NotifyChanged("Visited");
            }
        }
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                NotifyChanged("Text");
            }
        }
        public string Color
        {
            get { return color; }
            set
            {
                color = value;
                NotifyChanged("Color");
            }
        }
        public string Adjecent
        {
            get 
            {
                string[] temp = new string[adjacencyList.Count];
                for (int i = 0; i < adjacencyList.Count; i++)
                {
                    temp[i] = adjacencyList[i].Text;
                }
                NotifyChanged("adjacencyList");
                return string.Join(", ", temp);
            }
            set
            {
                Adjecent = value;
            }
        }
        public SampleVertex(string text)
        {
            Text = text;
            adjacencyList = new List<SampleVertex>();
        }
        public void Change()
        {
            Active = !Active;
            if (Active)
            {
                Color = "White";
            }
            else
            {
                Color = "Black";
            }
        }
        public void Visit()
        {
            Visited = !Visited;
            if (!Visited)
            {
                Color = "White";
                if (!Active)
                {
                    Color = "Black";
                }
            }
            else
            {
                Color = "Red";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
