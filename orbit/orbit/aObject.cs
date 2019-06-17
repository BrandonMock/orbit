using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Configuration;

namespace orbit
{
    public class aObject
    {
        public Guid id { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double ox { get; set; }
        public double oy { get; set; }
        public double vx { get; set; }
        public double vy { get; set; }
        public double mass { get; set; }

        public double vector { get; set; }
        private double splitPct { get; set; }
        public string text;
        public bool active = true;
        public Color color;
       
        public aObject()
        {
            id = Guid.NewGuid();
            x = 0;
            y = 0;
            ox = 0;
            oy = 0;
            vx = 0;
            vy = 0;
            mass = 0;
            text = null;
            active = true;
            splitPct = .004;
        }

        public override bool Equals (Object obj)
        {
            return ((obj is aObject) && ((aObject)obj).id == id);
        }


        public static bool operator ==(aObject x, aObject y)
        {
            return x.id == y.id;
        }

        public static bool operator !=(aObject x, aObject y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        bool PointsBetween(double x, double ox, double tx, double otx, double mass)
        {
            bool output = false;
            if (ox == 0 || otx == 0)
            {
                return false;
            }
            if (x < ox)
            {
                if (x <= tx && tx <= ox)
                {
                    output = true;
                }
            }
            else
            {
                if (ox <= tx && tx <= x)
                {
                    output = true;
                }
            }
            return output;
        }

        public List<aObject> Explode(double velocity = 0)
        {
            double tvx = vx;
            double tvy = vy;
            double tempMass = mass;

            double splitMass;

            splitMass =(double)((int)(mass * splitPct));

            Random seed = new Random((int)DateTime.UtcNow.Ticks);
            var templist = new List<aObject>();
            while (tempMass > 0)
            {
                

                int num = 0;
                if (tempMass > splitMass)
                {
                    Random rnd = new Random((int)tempMass + seed.Next(1,100));
                    num = rnd.Next((int)splitMass) + 1;
                } else
                {
                    Random rnd = new Random((int)tempMass);
                    num = rnd.Next((int)tempMass) + 1;
                }

                Random colorr = new Random((int)tempMass);
                var red = colorr.Next(0, 255);
                Random colorg = new Random((int)red);
                var green = colorg.Next(0, 255);
                Random colorb = new Random((int)green);
                var blue = colorb.Next(0, 255);

                var newobj = new aObject
                {
                    active = true,
                    mass = num,
                    x = 0,
                    y = 0,
                    ox = 0,
                    oy = 0,
                    color = Color.FromArgb(255, red , green , blue)
                };
                Random rnda = new Random((int)DateTime.UtcNow.Ticks / (blue+1));
                double rand = rnda.NextDouble()*(Math.PI * 2);
                double angle = (double)rand ;
                double Exforce = 0;
                if (velocity > 0)
                {
                    Exforce = -1 * velocity;
                } else
                {
                    Exforce = -1 * (vx+vy) ;
                }

                newobj.vx = newobj.vx + (double)(Math.Cos(angle) * Exforce);
                newobj.vy = newobj.vy + (double)(Math.Sin(angle) * Exforce);
                newobj.vector = angle;
                newobj.x = x + newobj.vx;
                newobj.y = y + newobj.vy;
                newobj.ox = x;
                newobj.oy = y;

                templist.Add(newobj);
                newobj = null;
                tempMass = tempMass - num;
                if (tempMass < 1)
                {
                    tempMass = 0;
                }
            }
            Destroy();
            return templist;
        }


        public void Copy(aObject a)
        {
            id = a.id;
            x = a.x;
            y = a.y;
            ox = a.ox;
            oy = a.oy;
            vx = a.vx;
            vy = a.vy;
            mass = a.mass;
            text = a.text;
            active = a.active;
        }

        public void Clear()
        {
            x = 0;
            y = 0;
            ox = 0;
            oy = 0;
            vx = 0;
            vy = 0;
            mass = 0;
            text = null;
            active = false;
        }


        public List<aObject> AttractAll(List<aObject> masterList)
        {
            List<aObject> listCopy = new List<aObject>();
            listCopy.AddRange(masterList);
            List<Task<aObjectList>> tasks = new List<Task<aObjectList>>();
            foreach (var oi in masterList)
            {
                if (oi.active && id != oi.id)
                {
                    Task<aObjectList> newtask = Task<aObjectList>.Factory.StartNew(() =>
                    {
                        aObjectList list = new aObjectList
                        {
                            data = Attract(oi)
                        };
                        return list;
                    });
                    tasks.Add(newtask);
                }  
            }
            Task.WaitAll();
            foreach(var t in tasks)
            {
                if (t.IsCompleted)
                {
                    if (t.Result.data != null)
                    {
                        if (listCopy == null)
                        {
                            listCopy.AddRange(t.Result.data);
                        }
                        else
                        {
                            listCopy.AddRange(t.Result.data.Except(listCopy));
                        }
                    }
                }
            }            
            return listCopy;
        }

        public List<aObject> Attract(aObject a)
        {
            double f = 0;
            double g = 0;
            double angle = 0;

            List<aObject> rval = null;

            if (PointsBetween(x, ox, a.x,a.ox, mass))
            {
                if (PointsBetween(y, oy,a.y, a.oy, mass))
                {
                    if (a.mass < mass)
                    {
                        if (a.mass < (mass * splitPct))
                        {
                            vx = ((vx / mass) + (a.vx / a.mass)) * (mass + a.mass);
                            vy = ((vy / mass) + (a.vy / a.mass)) * (mass + a.mass);
                            mass = mass + a.mass;
                            a.Destroy();
                        }
                        else
                        {
                            rval = a.Explode();
                        }
                    }
                    else
                    {
                        if (mass < (a.mass * splitPct))
                        {
                            a.vx = ((vx / mass) + (a.vx / a.mass)) * (mass + a.mass);
                            a.vy = ((vy / mass) + (a.vy / a.mass)) * (mass + a.mass);
                            a.mass = a.mass + mass;
                            Destroy();
                        }
                        else
                        {
                            rval = Explode();
                            return rval;
                        }
                    }
                }
            }

            
            double dist = getDistance(a);
            angle = getVector(a);

            g = 6.673 * (double)Math.Pow(10, -11);                                   // newtons gravitational constant
            f = (g * a.mass * mass ) / (Math.Pow(dist, 2));
            double fn = f * mass;
            vx = vx + (double)(Math.Cos(angle) * fn);
            vy = vy + (double)(Math.Sin(angle) * fn);

            if (Double.IsNaN(vx) || Double.IsNaN(vy))
            {
                //Console.WriteLine("anything");
            }
            return rval;
        }

        public void Destroy()
        {
            active = false;
        }

        public double getDistance(aObject a)
        {
            double distx = 0;
            double disty = 0;
            double dist = 0;

            distx = getDistx(a);
            disty = getDisty(a);
            dist = Math.Sqrt(Math.Pow(Math.Abs(distx), 2) + Math.Pow(Math.Abs(disty), 2));           // use a^2 + b^2 = c^2 to find angular distance
            if (dist < 1)
            {
                dist = 1;
            }
            return dist;
        }

        public double getDistx(aObject a)
        {
            double distx;
            distx = x - a.x;                                           // find absolute X distance
            if (distx == 0)
            {
                Random rnd1 = new Random((int)a.x * (int)DateTime.UtcNow.Ticks);
                distx = (rnd1.NextDouble() * .005) - .0025;
                x = x + distx;
            }
            return distx;
        }
        
        public double getDisty(aObject a)
        {
            double disty;
            disty = y - a.y;                                           // find absolute X distance
            if (disty == 0)
            {
                Random rnd1 = new Random((int)a.y * (int)DateTime.UtcNow.Ticks);
                disty = (rnd1.NextDouble() * .005) - .0025;
                y = y + disty;
            }
            return disty;
        }

        public double getVector(aObject a)
        {
            double angle = 0;

            angle = getWorkingAngle(a);
            int quadrant = getQuadrant(a);
            angle = (double)angle + ((double)quadrant * ((double)Math.PI * (double).5));
            return angle;
        }

        public int getQuadrant(aObject a)
        {
            int quadrant = 5;
            double distx = 0;
            double disty = 0;
            double angle;

            angle = getWorkingAngle(a);
            distx = getDistx(a);
            disty = getDisty(a);
            if ((distx < (double)0) == (disty < (double)0))
            {
                angle = Math.Atan(Math.Abs(disty) / Math.Abs(distx));
            }
            else
            {
                angle = Math.Atan(Math.Abs(distx) / Math.Abs(disty));
            }

            if (distx >= (double)0)
            {
                if (disty >= (double)0)
                {
                    quadrant = 2;
                }
                else
                {
                    quadrant = 1;
                }
            }
            else
            {
                if (disty >= (double)0)
                {
                    quadrant = 3;
                }
                else
                {
                    quadrant = 0;
                }
            }
            return quadrant;
        }
        private double getWorkingAngle(aObject a)
        {
            double angle;
                
            double distx = getDistx(a);
            double disty = getDisty(a);
            if ((distx < (double)0) == (disty < (double)0))
            {
                angle = Math.Atan(Math.Abs(disty) / Math.Abs(distx));
            }
            else
            {
                angle = Math.Atan(Math.Abs(distx) / Math.Abs(disty));
            }
            return angle;
        }

        public void recalcVector(aObject maxObject, int width, int height, double speedLimit)
        {
            if (active)
            {
                aObject testObj = new aObject
                {
                    x = x + vx,
                    y = y + vy
                };

                double vector = getVector(testObj);
                if (!Double.IsNaN(vector))
                {
                    int quadrant = getQuadrant(testObj);
                    double angle = vector - ((double)quadrant * ((double)Math.PI * (double).5));


                    double force = vx / Math.Cos(angle);
                    double forcey = vy / Math.Sin(angle);
                    if (Math.Abs(Math.Round(force, 3)) != Math.Abs(Math.Round(forcey, 3)))
                    {
                        force = vx / Math.Sin(angle);
                        forcey = vy / Math.Cos(angle);
                        if (Math.Abs(Math.Round(force, 3)) != Math.Abs(Math.Round(forcey, 3)))
                        {
                            //Console.WriteLine("{0} {1}", force, forcey);
                        }
                    }

                    if (Math.Abs(force) > speedLimit)
                    {
                        vx = (double)(Math.Cos(vector) * speedLimit);
                        vy = (double)(Math.Sin(vector) * speedLimit);
                    }
                    x = x + vx;
                    y = y + vy;
                }

                ///* aligns all objects with object 0 (keep 0 at center ) */
                x = x - ((maxObject.x - (width * .5)));
                y = y - ((maxObject.y - (height * .5)));
            }
        }
    }
}




