﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Slicer
{
    /// <summary>
    /// Interaction logic for ImageCreator.xaml
    /// </summary>
    public partial class ShapeCreator : UserControl, INotifyPropertyChanged
    {
        [DllImport("gdi32")]
        static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer, byte[] lpbBuffer);

        public ShapeCreator()
        {
            InitializeComponent();
        }

        public int ZoomFactor { get; set; } = 1;
        public double StrokeThin { get; set; } = 0.3;
        public double StrokeThick { get; set; } = 1;
        public double MouseX { get; set; }
        public double MouseY { get; set; }
        public double PolarLenght { get; set; }
        public double PolarAngle { get; set; }
        public ObservableCollection<string> SelectedCoordinates { get; set; } = new ObservableCollection<string>();
        public List<List<CutterPathItem>> CutterPath { get; set; } = new List<List<CutterPathItem>>();
        public Visibility Class2Visible { get; set; } = Visibility.Collapsed;
        public Visibility Class3Visible { get; set; } = Visibility.Collapsed;
        public Thickness CutterEmulatorPosition { get; set; } = new Thickness(0);
        public ImageSource PolarGridBackgroundImage { get; set; }
        public PointCollection CutterVisualizerPath { get; set; } = new PointCollection();
        public int OffsetX
        {
            get
            {
                return _OffsetX;
            }
            set
            {
                if (_OffsetX == value)
                    return;

                _OffsetX = value;
                setCutterVisualizerPath(OffsetX, OffsetY, true);
            }
        }
        public int OffsetY
        {
            get
            {
                return _OffsetY;
            }
            set
            {
                if (_OffsetY == value)
                    return;

                _OffsetY = value;
                setCutterVisualizerPath(OffsetX, OffsetY, true);
            }
        }


        private int _OffsetX;
        private int _OffsetY;
        private const double _DefaultStrokeThin = 0.3;
        private const double _DefaultStrokeThick = 1;
        private const int _MaximumZoomFactor = 512;
        private List<System.Windows.Point> Coordinates = new List<System.Windows.Point>();
        private List<Point> _SvgPathPoints = new List<Point>();

        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        

        private void ButtonZoom_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomFactor * 2 <= _MaximumZoomFactor)
            {
                ZoomFactor *= 2;
                StrokeThin /= 2;
                svImageCreator.ScrollToVerticalOffset(svImageCreator.VerticalOffset * 2);
                svImageCreator.ScrollToHorizontalOffset(svImageCreator.HorizontalOffset * 2);
            }
            else
            {
                ZoomFactor = 512;
                StrokeThin *= _DefaultStrokeThin / _MaximumZoomFactor;
            }

            if (ZoomFactor >= 4)
            {
                if (ZoomFactor >= 16)
                {
                    if (Class2Visible != Visibility.Visible)
                    {
                        Class2Visible = Visibility.Visible;
                        OnPropertyChanged("Class2Visible");
                    }

                    if (Class3Visible != Visibility.Visible)
                    {
                        Class3Visible = Visibility.Visible;
                        OnPropertyChanged("Class3Visible");
                    }
                }
                else
                {
                    if (Class2Visible != Visibility.Visible)
                    {
                        Class2Visible = Visibility.Visible;
                        OnPropertyChanged("Class2Visible");
                    }
                }
            }

            OnPropertyChanged("ZoomFactor");
            OnPropertyChanged("StrokeThin");
        }

        private void ButtonUnzoom_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomFactor / 2 >= 1)
            {
                ZoomFactor /= 2;
                StrokeThin *= 2;
                svImageCreator.ScrollToVerticalOffset(svImageCreator.VerticalOffset / 2);
                svImageCreator.ScrollToHorizontalOffset(svImageCreator.HorizontalOffset / 2);
            }
            else
            {
                ZoomFactor = 1;
                StrokeThin = _DefaultStrokeThin;
                svImageCreator.ScrollToVerticalOffset(0);
                svImageCreator.ScrollToHorizontalOffset(0);
            }

            if (ZoomFactor < 16)
            {
                if (ZoomFactor < 4)
                {
                    if (Class2Visible != Visibility.Collapsed)
                    {
                        Class2Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class2Visible");
                    }
                    if (Class3Visible != Visibility.Collapsed)
                    {
                        Class3Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class3Visible");
                    }
                }
                else
                {
                    if (Class3Visible != Visibility.Collapsed)
                    {
                        Class3Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class3Visible");
                    }
                }
            }

            OnPropertyChanged("ZoomFactor");
            OnPropertyChanged("StrokeThin");
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            MouseX = e.GetPosition(grdCoord).X;
            MouseY = e.GetPosition(grdCoord).Y;
            
            ((ToolTip)this.ToolTip).HorizontalOffset = e.GetPosition(svImageCreator).X + 5;
            ((ToolTip)this.ToolTip).VerticalOffset = e.GetPosition(svImageCreator).Y - 30;

            var centeredX = MouseX - 765;
            var centeredY = MouseY - 765;
            int quadrantAddition = 0;

            if (centeredX > 0 && centeredY > 0)
                quadrantAddition = -180;
            else if (centeredX > 0 && centeredY <= 0)
                quadrantAddition = 180;
                        
            if (centeredX == 0) //Avoid dividing by zero
                centeredX = 0.001;

            PolarLenght = Math.Sqrt(centeredX * centeredX + centeredY * centeredY);
            PolarAngle = (Math.Atan(centeredY / centeredX) * 180 / Math.PI) + quadrantAddition;

            OnPropertyChanged("MouseX");
            OnPropertyChanged("MouseY");
            OnPropertyChanged("PolarLenght");
            OnPropertyChanged("PolarAngle");
        }

        private void grdCoord_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AddCutterPathPoint(e.GetPosition(grdCoord).X, e.GetPosition(grdCoord).Y);

            //Set CutterEmulator Position
            CutterEmulatorPosition = new Thickness(
                (int)(e.GetPosition(grdCoord).X - elCutter.Width / 2),
                (int)(e.GetPosition(grdCoord).Y - elCutter.Height / 2),
                (int)(grdCoord.Width - e.GetPosition(grdCoord).X - elCutter.Width / 2),
                (int)(grdCoord.Height - e.GetPosition(grdCoord).Y - elCutter.Height / 2)
                );

            OnPropertyChanged("CutterEmulatorPosition");
        }

        /// <summary>
        /// Adds a new Point on the Path of the Cutter
        /// </summary>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        private void AddCutterPathPoint(double positionX, double positionY)
        {
            //get relative position to last point
            if (Coordinates.Count == 0)
            {
                //Set the Center as start postion
                Coordinates.Add(new Point(765, 765));
            }

            //add new Path, if not path exists
            if (CutterPath.Count == 0)
                CutterPath.Add(new List<CutterPathItem>());
            
            //Get last point
            var lastCenteredX = Coordinates.Last().X - 765;
            var lastCenteredY = Coordinates.Last().Y - 765;
            var lastCenteredLength = Math.Sqrt(lastCenteredX * lastCenteredX + lastCenteredY * lastCenteredY);

            var lastQudarant = 0;
            if (lastCenteredX >= 0 && lastCenteredY > 0)
                lastQudarant = 180;
            else if (lastCenteredX >= 0 && lastCenteredY <= 0)
                lastQudarant = 180;
            //else if (lastCenteredX < 0)
            //    lastQudarant = 360;

            if (lastCenteredX == 0)
                lastCenteredX = 0.001;

            var lastCenteredAngle = (Math.Atan(lastCenteredY / lastCenteredX) * 180 / Math.PI) + lastQudarant;



            //Get this actual point
            var thisCenteredX = positionX - 765;
            var thisCenteredY = positionY - 765;
            var thisCenteredLength = Math.Sqrt(thisCenteredX * thisCenteredX + thisCenteredY * thisCenteredY);

            var thisQudarant = 0;
            if (thisCenteredX >= 0 && thisCenteredY > 0)
                thisQudarant = 180;
            else if (thisCenteredX >= 0 && thisCenteredY <= 0)
                thisQudarant = 180;
            //else if (thisCenteredX < 0)
            //    thisQudarant = 360;

            if (thisCenteredX == 0)
                thisCenteredX = 0.001;

            var thisCenteredAngle = (Math.Atan(thisCenteredY / thisCenteredX) * 180 / Math.PI) + thisQudarant;

            //Get the "Field" where the last and current positions are
            var lastStepLength = Math.Floor(lastCenteredLength);
            var thisStepLength = Math.Floor(thisCenteredLength);
            var lastStepAngle = Math.Floor(lastCenteredAngle * 10);
            var thisStepAngle = Math.Floor(thisCenteredAngle * 10);


            //Get delta steps
            var deltaStepsLength = thisStepLength - lastStepLength;
            var deltaStepsAngle = thisStepAngle - lastStepAngle;

            //Get the shortest way for the rotor:
            if (deltaStepsAngle > 1800)
                deltaStepsAngle = (deltaStepsAngle - 3600);
            else if (deltaStepsAngle < -1800)
                deltaStepsAngle = (deltaStepsAngle + 3600);
            
            //Add currect position to path record
            Coordinates.Add(new Point(positionX, positionY));
            CutterPath.Last().Add(new CutterPathItem((int)deltaStepsAngle, (int)deltaStepsLength));

            //For tooltip
            SelectedCoordinates.Add(Math.Round(positionX, 2) + "/" + Math.Round(positionY, 2) +
                "   " + deltaStepsLength + " Steps Slide/ " + deltaStepsAngle + " Steps Rotor");

            //update GUI
            OnPropertyChanged("SelectedCoordinates");
            
        }

        /// <summary>
        /// Creates the Bytes that describes the shape (="CutterCode")
        /// </summary>
        private void GenerateCutterCode()
        {
            List<byte> cutterCommands = new List<byte>();

            //Add the Header   
            cutterCommands.Add(0x48); //Header
            cutterCommands.Add(0x75); //Header
            cutterCommands.Add(0x6E); //Header
            cutterCommands.Add(0x76); //Header

            foreach (var aCutterPath in CutterPath)
            {
                //For each part of the Cutterpath
                foreach (var aPoint in aCutterPath)
                {
                    //On first item, Adjust Slice to the start point
                    if (aPoint == aCutterPath.First())
                    {
                        //Do a absolute calibration and not relative
                        var initAngle = GetInitialAngleBytes(aPoint.DeltaAngle);
                        var initLength = GetInitialSlideBytes(aPoint.DeltaLenght);

                        cutterCommands.Add(initAngle.Value); //Amount of Rotation
                        cutterCommands.Add(initAngle.Key); //Command of Rotation
                        cutterCommands.Add(initLength.Value); //Amount of Slide
                        cutterCommands.Add(initLength.Key); //Command of Slide

                        //Knife down
                        cutterCommands.Add(0x0);
                        cutterCommands.Add((byte)CutterCode.KnifeDown);
                        continue;
                    }

                    var slide = GetDeltaSlideBytes(aPoint.DeltaLenght);
                    var rotor = GetDeltaAngleBytes(aPoint.DeltaAngle);
                    
                    if (rotor[0] > 0)
                        cutterCommands.AddRange(rotor);
                    if (slide[0] > 0)
                        cutterCommands.AddRange(slide);
                }


                //Knife up, when cutting of the part is done
                cutterCommands.Add(0);
                cutterCommands.Add((byte)CutterCode.KnifeUp);
            }

            //Footer:
            cutterCommands.Add(0);
            cutterCommands.Add((byte)CutterCode.Finish);

            //Save the bytes to file
            SaveCutterCode(cutterCommands.ToArray());
        }

        /// <summary>
        /// Saves the given ByteArray to a file
        /// </summary>
        /// <param name="cutterCode"></param>
        private void SaveCutterCode(byte[] cutterCode)
        {
            var sFD = new SaveFileDialog();
            if (sFD.ShowDialog() == true)
            {
                try
                {
                    var sW = new System.IO.FileStream(sFD.FileName, System.IO.FileMode.Create);
                    sW.Write(cutterCode, 0, cutterCode.Length);
                    sW.Close();

                    MessageBox.Show("Saved");
                }
                catch (Exception ea)
                {
                    MessageBox.Show("Unable to save CutterCodeFile. Reason: " + ea.Message);
                }
            }

            
        }

        /// <summary>
        /// Gets the command for the slide as a delta for the given angle
        /// </summary>
        /// <param name="angleSteps">the amount of steps to do for the rotor</param>
        /// <returns>A list of bytes. The List contains a set of Command byte and Amount byte. If steps are more than 255, there will be a list of 4 bytes etc.</returns>
        private List<byte> GetDeltaAngleBytes(int angleSteps) 
        {
            //Return of the byte for the given angle
            var cutCodeBytes = new List<byte>();

            //Take the shortest way
            if (angleSteps > 1800)
                angleSteps -= 3600;
            else if (angleSteps < -1800)
                angleSteps += 3600;

            //If the steps are more than one command can parse, add additional commands for the steps
            while (angleSteps > 255 || angleSteps < -255)
            {
                var factor = angleSteps < 0 ? -1 : 1;                
                cutCodeBytes.AddRange(GetDeltaAngleBytes(255 * factor));
                angleSteps -= 255 * factor;
            }            

            //Get the values for CMD Byte and Amount byte
            var cmdByte = angleSteps < 0 ? (byte)CutterCode.TurnCW :(byte)CutterCode.TurnCCW;
            var stepByte = angleSteps < 0 ? (byte)(angleSteps * -1) : (byte)angleSteps;

            //Add the bytes to return code
            cutCodeBytes.Add(stepByte);
            cutCodeBytes.Add(cmdByte);

            return cutCodeBytes;
        }

        /// <summary>
        /// Gets the command for the slide as a delta for the given lenght
        /// </summary>
        /// <param name="lengthSteps">the amount of steps to do for the slide</param>
        /// <returns>A list of bytes. The List contains a set of Command byte and Amount byte. If steps are more than 255, there will be a list of 4 bytes etc.</returns>
        private List<byte> GetDeltaSlideBytes(int lengthSteps)
        {
            var cutCodeBytes = new List<byte>();

            while (lengthSteps > 255 || lengthSteps < -255)
            {
                var factor = lengthSteps < 0 ? -1 : 1;
                cutCodeBytes.AddRange(GetDeltaSlideBytes(255 * factor));
                lengthSteps -= 255;
            }

            var cmdByte = (lengthSteps > 0 ? (byte)CutterCode.SledgeOut : (byte)CutterCode.SledgeIn);
            var stepByte = (lengthSteps < 0 ? (byte)(lengthSteps*-1) : (byte)lengthSteps);

            cutCodeBytes.Add(stepByte);
            cutCodeBytes.Add(cmdByte);

            return cutCodeBytes;
        }

        /// <summary>
        /// Gets the Rotorposition for the given Steps as absolute position
        /// Usually only used on start
        /// </summary>
        /// <param name="angleSteps">The steps of the rotor to do (1 step = 0.1°)</param>
        /// <returns>A KVP of CommandByte (Key) and AmountByte (Value)</returns>
        private KeyValuePair<byte, byte> GetInitialAngleBytes(int angleSteps) //returns <CmdByte, AmountByte>
        {
            var cmdByte = (byte)CutterCode.StartTurnCCW25;//Set to 0x80
            if (angleSteps < 0)
            {
                cmdByte = (byte)CutterCode.StartTurnCW25; //Change to 0x40 if CCW rotation is required
                angleSteps *= -1;
            }
                        
            while (angleSteps > 255)
            {
                cmdByte++; //Increase the cmdByte to the next 25,5°
                angleSteps -= 255;
            }
            //Ste the Steps for the Angle
            var stepByte = (byte)angleSteps;

            //Return the angle
            return new KeyValuePair<byte, byte>(cmdByte, stepByte);
        }

        /// <summary>
        /// Gets the Slideposition for the given Steps as absolute position
        /// Usually only used on start
        /// </summary>
        /// <param name="lengthSteps">The steps of the slider to do (1 step = 1/255 of an inch)</param>
        /// <returns>A KVP of CommandByte (Key) and AmountByte (Value)</returns>
        private KeyValuePair<byte, byte> GetInitialSlideBytes(int lengthSteps) //reurns <CmdByte, AmountByte>
        {
            var cmdByte = (byte)CutterCode.StartSledgeAndWaitForGoCenter; //Set Postion to Center (0x90)

            if (lengthSteps < 0)
                lengthSteps *= -1;

            //increase the cmdByte as long ans there is the value above one byte
            while(lengthSteps > 255)
            {
                cmdByte++;
                lengthSteps -= 255;
            }

            var stepByte = (byte)lengthSteps;

            return new KeyValuePair<byte, byte>(cmdByte, stepByte);
        }

        private void btnGenCutterCode_Click(object sender, RoutedEventArgs e)
        {
            GenerateCutterCode();            
        }

        private void btnKnifeDown_Click(object sender, RoutedEventArgs e)
        {
            //Todo
        }

        private void btnKnifeUp_Click(object sender, RoutedEventArgs e)
        {
            //Todo
            
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CutterEmulatorPosition = new Thickness(
                grdCoord.Width / 2 - elCutter.Width/2, 
                grdCoord.Height / 2 - elCutter.Height/2, 
                grdCoord.Width / 2 - elCutter.Width/2, 
                grdCoord.Height / 2 - elCutter.Height/2
                );
            OnPropertyChanged("CutterEmulatorPosition");
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W ||
                e.Key == Key.S ||
                e.Key == Key.A ||
                e.Key == Key.D)
            {
                switch (e.Key)
                {
                    case Key.W:
                        CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top - 1, CutterEmulatorPosition.Right, CutterEmulatorPosition.Bottom + 1);
                        break;
                    case Key.S:
                        CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top + 1, CutterEmulatorPosition.Right, CutterEmulatorPosition.Bottom - 1);
                        break;
                    case Key.A:
                        CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left - 1, CutterEmulatorPosition.Top, CutterEmulatorPosition.Right + 1, CutterEmulatorPosition.Bottom);
                        break;
                    case Key.D:
                        CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left + 1, CutterEmulatorPosition.Top, CutterEmulatorPosition.Right - 1, CutterEmulatorPosition.Bottom);
                        break;
                }
                AddCutterPathPoint(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top);
                OnPropertyChanged("CutterEmulatorPosition");
            }
        }

        private void btnLoadBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.gif";
            if (oFD.ShowDialog() == true)
            {
                try
                {                    
                    var bitmapImage = new BitmapImage(new Uri(oFD.FileName));
                    PolarGridBackgroundImage = bitmapImage;
                    OnPropertyChanged("PolarGridBackgroundImage");
                }
                catch (Exception ea)
                {
                    MessageBox.Show("Unable to load image. Maybe it is a unsupported format." + System.Environment.NewLine + "Errormessage: " + ea.Message);
                }
            }
        }
        

        private void btnLoadSVG_Click(object sender, RoutedEventArgs e)
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "SVG File|*.svg";
            if (oFD.ShowDialog() == true)
            {
                //Load SVG File
                var xDoc = new XmlDocument();
                xDoc.Load(oFD.FileName);

                var gNode = xDoc.DocumentElement;
                
                foreach (XmlNode aPath in gNode["g"].ChildNodes)
                {
                    if (aPath.Name.ToLower() == "path" && aPath.Attributes["d"] != null)
                    {
                        //Load the path and add it to Image Path
                        loadSvgPath(aPath.Attributes["d"].InnerText, false);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the Path from SVG file
        /// </summary>
        /// <param name="svgPathString"></param>
        /// <param name="overwrite"></param>
        private void loadSvgPath(string svgPathString, bool overwrite)
        {
            //JA
            //Testquader
            //string svgPathString = "M500.0 500.0C500.0 500.0 500.177617073 500.0 500.499374564 500.0C500.639807271 500.0 500.80769773 500.0 501.000262636 500.0C501.151963965 500.0 501.318978704 500.0 501.499946043 500.0C501.656542763 500.0 501.823587168 500.0 502.000197508 500.0C502.159109258 500.0 502.325765755 500.0 502.499524659 500.0C502.660301106 500.0 502.827158292 500.0 502.999587365 500.0C503.161739211 500.0 503.32881854 500.0 503.500402171 500.0C503.66315086 500.0 503.829951935 500.0 504.000444279 500.0C504.163604111 500.0 504.330144531 500.0 504.499749039 500.0C504.663892478 500.0 504.830905861 500.0 505.000502286 500.0C505.164655357 500.0 505.331228321 500.0 505.499961024 500.0C505.664740869 500.0 505.831580448 500.0 506.000237464 500.0C506.164919548 500.0 506.331334413 500.0 506.499256493 500.0C506.66462282 500.0 506.831450833 500.0 506.99952511 500.0C507.164951294 500.0 507.331584782 500.0 507.499220173 500.0C507.664818203 500.0 507.831393932 500.0 507.998749359 500.0C508.164621238 500.0 508.331259053 500.0 508.498470024 500.0C508.664689866 500.0 508.831476088 500.0 508.998639317 500.0C509.16523886 500.0 509.332212872 500.0 509.499373889 500.0C509.665700904 500.0 509.832213063 500.0 509.998725693 500.0C510.165365824 500.0 510.332006427 500.0 510.498462404 500.0C510.665265299 500.0 510.831882799 500.0 510.998128647 500.0C511.165437152 500.0 511.33236924 500.0 511.498735058 500.0C511.666177286 500.0 511.833045893 500.0 511.99914732 500.0C512.166788937 500.0 512.33364908 500.0 512.499528754 500.0C512.667254283 500.0 512.833977401 500.0 512.999492397 500.0C513.16746996 500.0 513.334203184 500.0 513.499477039 500.0C513.668021938 500.0 513.835049128 500.0 514.000330557 500.0C514.168941019 500.0 514.335734686 500.0 514.500469447 500.0C514.669502591 500.0 514.836367981 500.0 515.000804055 500.0C515.170338972 500.0 515.33729158 500.0 515.501375222 500.0C515.671075799 500.0 515.837707626 500.0 516.000953589 500.0C516.17138753 500.0 516.338130872 500.0 516.500822738 500.0C516.67204043 500.0 516.838770888 500.0 517.000593475 500.0C517.172898671 500.0 517.339639554 500.0 517.500308335 500.0C517.674260107 500.0 517.841094275 500.0 518.000166409 500.0C518.176588596 500.0 518.343463194 500.0 518.499911072 500.0C518.681105322 500.0 518.848313454 500.0 519.0001697 500.0C519.191952324 500.0 519.359249111 500.0 519.499308957 500.0C519.821898409 500.0 520.0 500.0 520.0 500.0C520.0 500.0 520.0 500.177617073 520.0 500.499374564C520.0 500.639807271 520.0 500.80769773 520.0 501.000262636C520.0 501.151963965 520.0 501.318978704 520.0 501.499946043C520.0 501.656542763 520.0 501.823587168 520.0 502.000197508C520.0 502.159109258 520.0 502.325765755 520.0 502.499524659C520.0 502.660301106 520.0 502.827158292 520.0 502.999587365C520.0 503.161739211 520.0 503.32881854 520.0 503.500402171C520.0 503.66315086 520.0 503.829951935 520.0 504.000444279C520.0 504.163604111 520.0 504.330144531 520.0 504.499749039C520.0 504.663892478 520.0 504.830905861 520.0 505.000502286C520.0 505.164655357 520.0 505.331228321 520.0 505.499961024C520.0 505.664740869 520.0 505.831580448 520.0 506.000237464C520.0 506.164919548 520.0 506.331334413 520.0 506.499256493C520.0 506.66462282 520.0 506.831450833 520.0 506.99952511C520.0 507.164951294 520.0 507.331584782 520.0 507.499220173C520.0 507.664818203 520.0 507.831393932 520.0 507.998749359C520.0 508.164621238 520.0 508.331259053 520.0 508.498470024C520.0 508.664689866 520.0 508.831476088 520.0 508.998639317C520.0 509.16523886 520.0 509.332212872 520.0 509.499373889C520.0 509.665700904 520.0 509.832213063 520.0 509.998725693C520.0 510.165365824 520.0 510.332006427 520.0 510.498462404C520.0 510.665265299 520.0 510.831882799 520.0 510.998128647C520.0 511.165437152 520.0 511.33236924 520.0 511.498735058C520.0 511.666177286 520.0 511.833045893 520.0 511.99914732C520.0 512.166788937 520.0 512.33364908 520.0 512.499528754C520.0 512.667254283 520.0 512.833977401 520.0 512.999492397C520.0 513.16746996 520.0 513.334203184 520.0 513.499477039C520.0 513.668021938 520.0 513.835049128 520.0 514.000330557C520.0 514.168941019 520.0 514.335734686 520.0 514.500469447C520.0 514.669502591 520.0 514.836367981 520.0 515.000804055C520.0 515.170338972 520.0 515.33729158 520.0 515.501375222C520.0 515.671075799 520.0 515.837707626 520.0 516.000953589C520.0 516.17138753 520.0 516.338130872 520.0 516.500822738C520.0 516.67204043 520.0 516.838770888 520.0 517.000593475C520.0 517.172898671 520.0 517.339639554 520.0 517.500308335C520.0 517.674260107 520.0 517.841094275 520.0 518.000166409C520.0 518.176588596 520.0 518.343463194 520.0 518.499911072C520.0 518.681105322 520.0 518.848313454 520.0 519.0001697C520.0 519.191952324 520.0 519.359249111 520.0 519.499308957C520.0 519.821898409 520.0 520.0 520.0 520.0C520.0 520.0 519.822382927 520.0 519.500625436 520.0C519.360192729 520.0 519.19230227 520.0 518.999737364 520.0C518.848036035 520.0 518.681021296 520.0 518.500053957 520.0C518.343457237 520.0 518.176412832 520.0 517.999802492 520.0C517.840890742 520.0 517.674234245 520.0 517.500475341 520.0C517.339698894 520.0 517.172841708 520.0 517.000412635 520.0C516.838260789 520.0 516.67118146 520.0 516.499597829 520.0C516.33684914 520.0 516.170048065 520.0 515.999555721 520.0C515.836395889 520.0 515.669855469 520.0 515.500250961 520.0C515.336107522 520.0 515.169094139 520.0 514.999497714 520.0C514.835344643 520.0 514.668771679 520.0 514.500038976 520.0C514.335259131 520.0 514.168419552 520.0 513.999762536 520.0C513.835080452 520.0 513.668665587 520.0 513.500743507 520.0C513.33537718 520.0 513.168549167 520.0 513.00047489 520.0C512.835048706 520.0 512.668415218 520.0 512.500779827 520.0C512.335181797 520.0 512.168606068 520.0 512.001250641 520.0C511.835378762 520.0 511.668740947 520.0 511.501529976 520.0C511.335310134 520.0 511.168523912 520.0 511.001360683 520.0C510.83476114 520.0 510.667787128 520.0 510.500626111 520.0C510.334299096 520.0 510.167786937 520.0 510.001274307 520.0C509.834634176 520.0 509.667993573 520.0 509.501537596 520.0C509.334734701 520.0 509.168117201 520.0 509.001871353 520.0C508.834562848 520.0 508.66763076 520.0 508.501264942 520.0C508.333822714 520.0 508.166954107 520.0 508.00085268 520.0C507.833211063 520.0 507.66635092 520.0 507.500471246 520.0C507.332745717 520.0 507.166022599 520.0 507.000507603 520.0C506.83253004 520.0 506.665796816 520.0 506.500522961 520.0C506.331978062 520.0 506.164950872 520.0 505.999669443 520.0C505.831058981 520.0 505.664265314 520.0 505.499530553 520.0C505.330497409 520.0 505.163632019 520.0 504.999195945 520.0C504.829661028 520.0 504.66270842 520.0 504.498624778 520.0C504.328924201 520.0 504.162292374 520.0 503.999046411 520.0C503.82861247 520.0 503.661869128 520.0 503.499177262 520.0C503.32795957 520.0 503.161229112 520.0 502.999406525 520.0C502.827101329 520.0 502.660360446 520.0 502.499691665 520.0C502.325739893 520.0 502.158905725 520.0 501.999833591 520.0C501.823411404 520.0 501.656536806 520.0 501.500088928 520.0C501.318894678 520.0 501.151686546 520.0 500.9998303 520.0C500.808047676 520.0 500.640750889 520.0 500.500691043 520.0C500.178101591 520.0 500.0 520.0 500.0 520.0C500.0 520.0 500.0 519.822382927 500.0 519.500625436C500.0 519.360192729 500.0 519.19230227 500.0 518.999737364C500.0 518.848036035 500.0 518.681021296 500.0 518.500053957C500.0 518.343457237 500.0 518.176412832 500.0 517.999802492C500.0 517.840890742 500.0 517.674234245 500.0 517.500475341C500.0 517.339698894 500.0 517.172841708 500.0 517.000412635C500.0 516.838260789 500.0 516.67118146 500.0 516.499597829C500.0 516.33684914 500.0 516.170048065 500.0 515.999555721C500.0 515.836395889 500.0 515.669855469 500.0 515.500250961C500.0 515.336107522 500.0 515.169094139 500.0 514.999497714C500.0 514.835344643 500.0 514.668771679 500.0 514.500038976C500.0 514.335259131 500.0 514.168419552 500.0 513.999762536C500.0 513.835080452 500.0 513.668665587 500.0 513.500743507C500.0 513.33537718 500.0 513.168549167 500.0 513.00047489C500.0 512.835048706 500.0 512.668415218 500.0 512.500779827C500.0 512.335181797 500.0 512.168606068 500.0 512.001250641C500.0 511.835378762 500.0 511.668740947 500.0 511.501529976C500.0 511.335310134 500.0 511.168523912 500.0 511.001360683C500.0 510.83476114 500.0 510.667787128 500.0 510.500626111C500.0 510.334299096 500.0 510.167786937 500.0 510.001274307C500.0 509.834634176 500.0 509.667993573 500.0 509.501537596C500.0 509.334734701 500.0 509.168117201 500.0 509.001871353C500.0 508.834562848 500.0 508.66763076 500.0 508.501264942C500.0 508.333822714 500.0 508.166954107 500.0 508.00085268C500.0 507.833211063 500.0 507.66635092 500.0 507.500471246C500.0 507.332745717 500.0 507.166022599 500.0 507.000507603C500.0 506.83253004 500.0 506.665796816 500.0 506.500522961C500.0 506.331978062 500.0 506.164950872 500.0 505.999669443C500.0 505.831058981 500.0 505.664265314 500.0 505.499530553C500.0 505.330497409 500.0 505.163632019 500.0 504.999195945C500.0 504.829661028 500.0 504.66270842 500.0 504.498624778C500.0 504.328924201 500.0 504.162292374 500.0 503.999046411C500.0 503.82861247 500.0 503.661869128 500.0 503.499177262C500.0 503.32795957 500.0 503.161229112 500.0 502.999406525C500.0 502.827101329 500.0 502.660360446 500.0 502.499691665C500.0 502.325739893 500.0 502.158905725 500.0 501.999833591C500.0 501.823411404 500.0 501.656536806 500.0 501.500088928C500.0 501.318894678 500.0 501.151686546 500.0 500.9998303C500.0 500.808047676 500.0 500.640750889 500.0 500.500691043C500.0 500.178101591 500.0 500.0 500.0 500.0";

            //Add a new Path
            CutterPath.Add(new List<CutterPathItem>());

            //Remove the letters and get the coordinates 
            svgPathString = svgPathString.Replace('C', ' ').TrimStart('M');
            var svgPathStringArray = svgPathString.Split(' ');

            //Remove the Period
            for (var i = 0; i < svgPathStringArray.Length; i++)
            {
                svgPathStringArray[i] = svgPathStringArray[i].Split('.')[0];
            }

            if (overwrite) _SvgPathPoints.Clear();

            //Convert to Points for Grid
            for (var i = 0; i < svgPathStringArray.Length; i += 2)
            {
                _SvgPathPoints.Add(new Point(Convert.ToInt32(svgPathStringArray[i]) + 255, Convert.ToInt32(svgPathStringArray[i + 1]) + 255));
            }

            //Remove Points that are existing twice
            for (var i = 0; i < _SvgPathPoints.Count - 1; i++)
            {
                if (_SvgPathPoints[i].X == _SvgPathPoints[i + 1].X && _SvgPathPoints[i].Y == _SvgPathPoints[i + 1].Y)
                {
                    _SvgPathPoints.RemoveAt(i + 1);
                    i--;
                }
            }

            setCutterVisualizerPath(OffsetX,OffsetY, overwrite);
        }

        /// <summary>
        /// Draws the Path of the cutter including the offset
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        private void setCutterVisualizerPath(int offsetX, int offsetY, bool overwrite)
        {
            var offsetSvgPath = new Point[_SvgPathPoints.Count];
            _SvgPathPoints.CopyTo(offsetSvgPath);

            if (overwrite)
            {
                Coordinates.Clear();
                CutterPath.Clear();
                SelectedCoordinates.Clear();
            }

            for (var i = 0; i < offsetSvgPath.Length; i++)
            {
                offsetSvgPath[i].X += offsetX;
                offsetSvgPath[i].Y += offsetY;
                AddCutterPathPoint(offsetSvgPath[i].X + offsetX, offsetSvgPath[i].Y + offsetY);
            }


            CutterVisualizerPath = new PointCollection(offsetSvgPath);
            OnPropertyChanged("CutterVisualizerPath");
        }   
        
    }
}