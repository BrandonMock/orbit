using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;


namespace orbit
{
    public partial class Form1 : Form
    {
        long cnt;
        double zoomFactor;
        double ozoomFactor;
        public bool stop;
        public long lastCount;
        List<aObject> masterList;
        double speedLimit;
        int waitTime;

        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }
        public Form1()
        {
            stop = false;
            InitializeComponent();

            //Visible = true;
            //BringToFront();
        }

        private aObject getFocusObject(List<aObject> masterList)
        {
            return masterList.OrderByDescending(item => item.mass).First();
        }

        private aObject getFocusObject(List<aObject> masterList, int id)
        {
            return masterList.OrderByDescending(item => item.mass).ElementAt(id);
        }

        public void Placetext(List<aObject> objects, bool Plot = true)
        {
            
            if (Plot)
            {
                if (ozoomFactor != zoomFactor)
                {
                    this.Refresh();
                }
                ozoomFactor = zoomFactor;
            }

            var G = this.CreateGraphics();
            var maxObject = getFocusObject(objects);
            foreach (var o in objects)
            {
                if (Plot && o.id != maxObject.id)
                {
                    long lx;
                    long ly;
                    long lMass = 10;  //(int)((double)o.mass / ((double)zoomFactor));
                    long lMass2 = 0;
                    if (lMass != 0)
                    {
                        lMass2 = lMass / 2;
                    }
                    else
                    {
                        lMass = 1;
                    }

                    lx = (int)((o.ox) / ozoomFactor) + (Width / 2);
                    ly = (int)((o.oy) / ozoomFactor) + (Height / 2);

                    if (lx > 0 && lx < this.Width && ly > 0 && ly < this.Height)
                    {

                        Brush b = new Pen(Color.Black).Brush;
                        Rectangle R = new Rectangle(
                            (int)(lx - lMass2),
                            (int)(ly - lMass2),
                            (int)lMass,
                            (int)lMass
                        );

                        G.FillEllipse(b, R);

                    }

                    o.ox = o.x;
                    o.oy = o.y;

                    lx = (int)((o.x) / zoomFactor) + (Width / 2);
                    ly = (int)((o.y) / zoomFactor) + (Height / 2);

                    if (lx > 0 && lx < this.Width && ly > 0 && ly < this.Height)
                    {

                        Brush b = new Pen(o.color).Brush;
                        Rectangle R = new Rectangle(
                            (int)(lx - lMass2),
                            (int)(ly - lMass2),
                            (int)lMass,
                            (int)lMass
                        );
                        
                        G.FillEllipse(b, R);

                    }
                }
            }
            //
        }

        public void DoStuff()
        {       
            cnt++;
            Visible = true;
            BringToFront();

            masterList.RemoveAll(r => r.active == false);
            var maxObject = getFocusObject(masterList);
            List<aObject> templist = new List<aObject>();
            foreach (aObject oo in masterList)
            {
                var a = oo.AttractAll(masterList);
                if (templist == null)
                {
                    templist.AddRange(a);
                }
                else
                {
                    templist.AddRange(a.Except(templist));
                }
                
                oo.recalcVector(maxObject, Width, Height, speedLimit);
            }

            masterList = templist;
            

            Placetext(masterList, (double)(int)(cnt / waitTime) == (double)cnt / waitTime);
            lastCount = masterList.Count();
            masterList.RemoveAll(r => r.active == false);

            if ((double)(int)(cnt / waitTime) == (double)cnt / waitTime)
            {                
                this.label1.Text = masterList.Count().ToString();
                this.label2.Text = cnt.ToString();
                maxObject = getFocusObject(masterList, 1);
                this.label3.Text = maxObject.mass.ToString();
                
                label1.Refresh();
                label2.Refresh();
                label3.Refresh();

                var obj = masterList.Where(x=>x.active==true).OrderByDescending(x=> x.getDistance(maxObject)).FirstOrDefault();
                var maxX = Math.Abs(obj.getDistance(maxObject));
                int compare = (int)(Math.Abs(maxX) / (double)Width * 3);
                double factor = zoomFactor * .01 * waitTime;
                //if (factor < 1)
                //{
                //    factor = 1;
                //}

                if (zoomFactor > compare)
                {
                    if ((zoomFactor - factor) < compare)
                    {
                        //zoomFactor = compare;
                    } else {
                        zoomFactor = zoomFactor - factor;
                    }
                } else {
                    if ((zoomFactor + factor) > compare)
                    {
                        //zoomFactor = compare;
                    }
                    else
                    {
                        zoomFactor = zoomFactor + factor;
                    }
                }
                zoomFactor = Math.Round(zoomFactor, 2);
                numericUpDown1.Text = zoomFactor.ToString();

                numericUpDown1.Refresh();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            masterList = new List<aObject>();

            cnt = 0;
            waitTime = int.Parse(ConfigurationManager.AppSettings["waitTime"]);
            zoomFactor = int.Parse(ConfigurationManager.AppSettings["zoomFactor"]);
            speedLimit = double.Parse(ConfigurationManager.AppSettings["speedLimit"]);
            int startingMass = int.Parse(ConfigurationManager.AppSettings["startingMass"]);
            int centerMass = int.Parse(ConfigurationManager.AppSettings["centerMass"]);
            numericUpDown1.Text =  zoomFactor.ToString();
            //this.Visible = true;
            //this.BringToFront();


            aObject s = new aObject
            {
                mass = startingMass,
                x = 0,
                y = 0,
                ox = 0,
                oy = 0,
                vx = 0,
                vy = 0
            };

            masterList.Add(s);
            masterList.AddRange(masterList.First().Explode((double)speedLimit));
            masterList.First().active = true;
            masterList.First().mass = centerMass;
            masterList.First().color = Color.Black;
            //MainTimer_Tick(this, e);
            while (1 != 0)
            {
                DoStuff();
            }
        }



    }
}
