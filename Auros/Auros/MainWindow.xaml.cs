using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.Kinect;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Timers;

namespace Auros
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region KinectProperties
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;




        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }
        private string statusText = null;
        #endregion

        #region App Properties
        //class def
        JointManager jointManager;
        Serial gloveSerial;
        StringBuilder csvHeaderBuilder;
        StringBuilder csvDataBuilder;
        public List<Assessment> assessmentLibrary { get; set; }
        public object ApplicationDeployment { get; private set; }

        Assessment activeAssessment;
        Definitions.AssessSide activeSide; //TODO add side control

        /// <summary>
        /// row [0][i] - nama fitur
        /// row [1][i] - flag fitur
        /// </summary>
        string[][] activeFilter;

        //isolated storage
        IsolatedStorageFile isoStore;

        ///<summary>
        ///Harus Di assign di constructor, buat ngecek file juga
        ///</summary>   
        public Definitions.UserCode activeUser;

        Definitions.FunctionMode functionMode;
        Definitions.TrainingState trainingState;
        Definitions.ClassifyingState classifyingState;

        Stopwatch timerStep;
        bool isRecording;
        bool isTimeStepping;

        /// <summary>
        /// untuk memotong gerakan berulang dalam sekali assessment
        /// </summary>
        int currentTrimmingId;

        //labelling prop
        bool isLabellingFinish;
        Grid[] labellingGrids;
        ComboBox[] labellingCombos;
        TextBlock[] labellingText;
        string[][] tempScoreLabel;

        //body tracking
        ulong activeBodyIndex;
        int trackedBodyNum;

        //Emergency Properties
        private readonly System.Timers.Timer emergencyTimer;
        //int frameCount = 0;


        #endregion

        public MainWindow()
        {
            InitKinect();

            ClearElements();

            jointManager = new JointManager();
            csvHeaderBuilder = new StringBuilder();
            csvDataBuilder = new StringBuilder();

            InitLogin();

            assessmentLibrary = new List<Assessment>();
            InitAssessmentLibrary();
            activeFilter = new string[2][];

            isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null);
            InitFileStorage(Definitions.FunctionMode.Training.ToString());
            InitFileStorage(Definitions.FunctionMode.Classify.ToString());

            timerStep = new Stopwatch();
            isRecording = false;
            isTimeStepping = false;

            InitializeComponent();

            InitSerial();

            trackedBodyNum = 0;

            functionMode = Definitions.FunctionMode.Training;
            trainingState = Definitions.TrainingState.Video;
            classifyingState = Definitions.ClassifyingState.Video;

            InitView();
            InitLabellingPanel();

            //Emergency Test Here
            emergencyTimer = new System.Timers.Timer(200);
            emergencyTimer.Elapsed += new System.Timers.ElapsedEventHandler(EmergencyLoop);

        }

        #region Emergency Event
        /// <summary>
        /// Emergency Loop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmergencyLoop(object sender, ElapsedEventArgs e)
        {
            //Emergency Loop
            Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
            Array jointIteration = Enum.GetValues(typeof(JointType));
            foreach (var j in jointIteration)
            {
                System.Random r = new System.Random();
                Joint newJoint = new Joint();
                newJoint.Position.X = (float)999;
                newJoint.Position.Y = (float)999;
                newJoint.Position.Z = (float)999;
                joints.Add((JointType)j, newJoint);
            }
            IReadOnlyDictionary<JointType, Joint> roJoint = joints;
            FetchSensorData(roJoint);

        }
        /// <summary>
        /// Toggle emergency loop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayClick_Handler(object sender, RoutedEventArgs e)
        {
            var enabler = emergencyTimer.Enabled;
            emergencyTimer.Enabled = !(emergencyTimer.Enabled);
            if (enabler) EmergencyLoopButton.Content = "OFF";
            if (!enabler) EmergencyLoopButton.Content = "ON";
        }
        #endregion

        #region Init
        private void InitSerial()
        {
            gloveSerial = new Serial();
            bool serialPortOpened = gloveSerial.OpenPort(Definitions.PortNumber);
            if (serialPortOpened) PortText.Text = "COM" + Definitions.PortNumber.ToString() + " opened";

        }

        private void InitView()
        {
            string[] side = new string[2] { Definitions.AssessSide.Right.ToString(), Definitions.AssessSide.Left.ToString() };
            SideComboBox.ItemsSource = side;

            //SideComboBox.SelectedItem = side[0];

            AssessmentListView.DataContext = this;
            AssessmentListView.SelectedIndex = 0;
            activeAssessment = (Assessment)AssessmentListView.SelectedItem;
            functionMode = Definitions.FunctionMode.Training;

            popUpBar.Visibility = Visibility.Hidden;

            SettingGrid.Visibility = Visibility.Collapsed;
            ReportGrid.Visibility = Visibility.Collapsed;

            BigVideoPlayer.Volume = 0.0;
            SmallVideoPlayer.Volume = 0.0;
        }

        private void InitLabellingPanel()
        {

            isLabellingFinish = false;

            labellingGrids = new Grid[] { labelItem0, labelItem1, labelItem2, labelItem3, labelItem4, labelItem5 };
            labellingCombos = new ComboBox[] { labeValue0, labeValue1, labeValue2, labeValue3, labeValue4, labeValue5 };
            labellingText = new TextBlock[] { labeltext0, labeltext1, labeltext2, labeltext3, labeltext4, labeltext5 };

            foreach (ComboBox cbl in labellingCombos)
            {
                cbl.ItemsSource = Definitions.FMALabel;
            }

            labellingPanel.Visibility = Visibility.Hidden;
        }

        private void InitKinect()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;
        }

        private void InitUser()
        {


        }

        private void InitFileStorage(String FirstLevelDir)
        {
            try
            {
                foreach (Assessment ass in assessmentLibrary)
                {
                    string assDir = FirstLevelDir + "/Raw/" + ass.AssessmentCode.ToString() + "/" + activeUser.ToString();
                    if (!isoStore.DirectoryExists(assDir))
                        isoStore.CreateDirectory(assDir);
                    ass.RawDataPath = assDir;
                    //TODO Count available assessment data for the current user here

                    try
                    {
                        if (isoStore.FileExists(assDir + "/config.txt"))
                        {
                            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(assDir + "/config.txt", FileMode.Open, isoStore))
                            {
                                using (StreamReader reader = new StreamReader(isoStream))
                                {
                                    string sNM = reader.ReadToEnd().Split('.')[0];
                                    ass.storedRawDataNum = Convert.ToInt32(sNM);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("[Error]Cant read config file of >" + e.Message.ToString());
                    }

                    foreach (Item itm in ass.AssociatedItemList)
                    {
                        string itmDir = FirstLevelDir + "/Preproc/" + itm.ItemCode.ToString() + "/" + activeUser.ToString();
                        if (!isoStore.DirectoryExists(itmDir))
                            isoStore.CreateDirectory(itmDir);
                        itm.PreProcDataPath = itmDir;
                    }
                }
                Debug.WriteLine("[Success]Initializing folder");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Initializing folder >" + e.Message);
            }
        }

        private void InitLogin()
        {
            activeUser = Definitions.UserCode.Ajik;
            //activeSide = Definitions.AssessSide.Left;
            activeSide = Definitions.AssessSide.Right;
            activeBodyIndex = 0;
        }
        #endregion

        #region Kinect SDK    

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    int penIndex = 0;
                    //TODO Clear data buffer, Handle body lebih dari satu


                    //Body filter
                    {
                        double sagital = 9999.0;
                        double planar = 9999.0;
                        foreach (Body body in this.bodies)
                        {
                            if (body.IsTracked && Math.Abs(body.Joints[JointType.SpineBase].Position.X) < sagital && Math.Abs(body.Joints[JointType.SpineBase].Position.Z) < planar)
                            {
                                sagital = Math.Abs(body.Joints[JointType.SpineBase].Position.X);
                                activeBodyIndex = body.TrackingId;
                            }
                        }
                    }

                    foreach (Body body in this.bodies)
                    {

                        if (body.TrackingId == activeBodyIndex)
                        {
                            #region bodyDrawing
                            Pen drawPen = this.bodyColors[penIndex++];

                            if (body.IsTracked)
                            {
                                this.DrawClippedEdges(body, dc);

                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                //HACK Adjust data rate
                                //frameCount++;
                                //if (frameCount % 2 == 0) 
                                FetchSensorData(joints);

                                // convert the joint points to depth (display) space
                                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                                foreach (JointType jointType in joints.Keys)
                                {
                                    // sometimes the depth(Z) of an inferred joint may show as negative
                                    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                    CameraSpacePoint position = joints[jointType].Position;
                                    if (position.Z < 0)
                                    {
                                        position.Z = InferredZPositionClamp;
                                    }

                                    DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);

                                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                }

                                this.DrawBody(joints, jointPoints, dc, drawPen);

                                this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                            }

                            #endregion
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {

            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        #endregion        

        #region Data Control
        private void InitAssessmentLibrary()
        {
            List<Assessment> libraryDeleter = new List<Assessment>();
            try
            {
                Array AssessmentCodeList = Enum.GetValues(typeof(Definitions.AssessmentCode));
                int i = 0;
                foreach (var assCode in AssessmentCodeList)
                {
                    Assessment newAssesment = new Assessment();
                    newAssesment.AssessmentName = Definitions.AssessItemName[(int)assCode];
                    newAssesment.AssessmentCode = (Definitions.AssessmentCode)i;
                    assessmentLibrary.Add(newAssesment);
                    i++;
                }
                Debug.WriteLine("[Success]Creating assessment library");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Creating assessment library >" + e.Message);
            }

            try
            {
                string[] ItemEachAssessmentCodeLine = File.ReadAllLines(Definitions.ItemEachAssessmentPath);
                int i = 0;
                foreach (Assessment ass in assessmentLibrary)
                {
                    int j = 0;
                    foreach (string itm in ItemEachAssessmentCodeLine[i].Split(','))
                    {

                        if (ItemEachAssessmentCodeLine[i].Split(',')[7] != "exclude")
                        {
                            if (itm.Length == 3 && j != 0)
                            {
                                Item newItem = new Item();
                                newItem.ItemCode = (Definitions.ItemCode)Enum.Parse(typeof(Definitions.ItemCode), itm);
                                ass.AssociatedItemList.Add(newItem);
                                ass.isActive = true;
                            }
                            j++;
                        }
                        else
                        {
                            ass.isActive = false;
                            //delete data accs on library
                            libraryDeleter.Add(ass);
                            break;
                        }
                    }
                    i++;
                }

                foreach (Assessment dss in libraryDeleter)
                {
                    assessmentLibrary.Remove(dss);
                }

                var aas = assessmentLibrary;

                Debug.WriteLine("[Success]Load Item object to assessment library");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Load Item object to assessment library >" + e.Message);
            }

        }

        private void AssessmentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitTempData();

            if (functionMode == Definitions.FunctionMode.Training)
            {
                UpdateContent(trainingState);
            }
            else if (functionMode == Definitions.FunctionMode.Classify)
            {
                UpdateContent(classifyingState);
            }
            FocusManager.SetFocusedElement(MainGrid, DisplayGrid);
        }

        private void InitTempData()
        {
            try
            {
                File.Delete(Definitions.TempFileName);
                Debug.WriteLine("[Success]Delete temporary data ");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Cant delete temp file" + exc.Message);
            }
            activeAssessment = (Assessment)AssessmentListView.SelectedItem;

            string[] rawFilter = null;
            string[][] dataFilter = null;
            try
            {
                string filterPath = (activeSide == Definitions.AssessSide.Right) ? Definitions.RFeaturedDataEachAssessmentPath : Definitions.LFeaturedDataEachAssessmentPath;
                rawFilter = File.ReadAllLines(filterPath);
                dataFilter = new string[rawFilter.Length][];
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error] fail opening filter" + exc.Message);
            }

            int i = 0;
            foreach (string rf in rawFilter)
            {
                dataFilter[i] = rf.Split(',');
                if (dataFilter[i][0] == activeAssessment.AssessmentCode.ToString())
                {
                    activeFilter[0] = dataFilter[0];
                    activeFilter[1] = dataFilter[i];
                    break;
                }
                i++;
            }
            try
            {
                string dataHeader = activeFilter[0][1];
                for (i = 2; i < activeFilter[1].Length; i++)
                {
                    if (activeFilter[1][i] == "1")
                    {
                        dataHeader += "," + activeFilter[0][i];
                    }
                }
                csvHeaderBuilder = new StringBuilder();
                csvHeaderBuilder.AppendLine(dataHeader);
                File.AppendAllText(Definitions.TempFileName, csvHeaderBuilder.ToString());
                Debug.WriteLine("[Success]Creating Filter and temporary data ");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Creating Filter and temporary data >" + exc.Message);
            }
        }

        private void FetchSensorData(IReadOnlyDictionary<JointType, Joint> fJoints)
        {
            string dataChunk = string.Empty;
            if (isRecording)
            {
                try
                {
                    string[] gloveSensorData = null;
                    if (!emergencyTimer.Enabled && (activeAssessment.AssessmentCode != Definitions.AssessmentCode.U21 && activeAssessment.AssessmentCode != Definitions.AssessmentCode.U31))
                    {
                        try
                        {
                            string gloveSensorDataRaw = gloveSerial.ReadPort();
                            ///<summary>
                            ///0 - flex
                            ///1 - force
                            ///2 - ax
                            ///3 - ay
                            ///4 - az
                            ///5 - gx
                            ///6 - gy
                            ///7 - gz
                            /// </summary>
                            gloveSensorData = gloveSensorDataRaw.Split('#');
                            gloveSensorData[7] = gloveSensorData[7].Split('\r')[0];
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine("[Error] data glove unavailable >" + exc.Message);

                        }
                    }
                    else
                    {
                        gloveSensorData = new string[8];
                        for (int i = 0; i < gloveSensorData.Length; i++)
                        {
                            gloveSensorData[i] = "111";
                        }
                    }

                    if (gloveSensorData.Length == 8 && fJoints != null) //glove data validation to avoid lead time
                    {
                        //toggle time step
                        if (!isTimeStepping)
                        {
                            isTimeStepping = !isTimeStepping;
                            timerStep.Start();
                        }
                        dataChunk += timerStep.ElapsedMilliseconds.ToString();
                        for (int i = 2; i < activeFilter[1].Length; i++) //cek flag mulai dari setelah time stamp ke kanan
                        {
                            string apData = string.Empty;
                            if (activeFilter[1][i] == "1")
                            {
                                if (activeFilter[0][i] == "Flex" || activeFilter[0][i] == "Force" || activeFilter[0][i] == "Accel" || activeFilter[0][i] == "Gyro")
                                {
                                    switch (activeFilter[0][i])
                                    {
                                        case "Flex":
                                            apData = gloveSensorData[0];
                                            break;
                                        case "Force":
                                            apData = gloveSensorData[1];
                                            break;
                                        case "Accel":
                                            apData = gloveSensorData[2] + ";" + gloveSensorData[3] + ";" + gloveSensorData[4];
                                            break;
                                        case "Gyro":
                                            apData = gloveSensorData[5] + ";" + gloveSensorData[6] + ";" + gloveSensorData[7];
                                            break;
                                    }
                                }
                                else if (activeFilter[0][i] == "TrimmingId")
                                {
                                    apData = currentTrimmingId.ToString();
                                }
                                else
                                {
                                    JointType selJoint;
                                    Enum.TryParse(activeFilter[0][i], out selJoint);
                                    apData += (fJoints[selJoint].Position.X.ToString() + ";" + fJoints[selJoint].Position.Y.ToString() + ";" + fJoints[selJoint].Position.Z.ToString());

                                }
                                dataChunk += ("," + apData);
                            }
                            else
                            {
                                //ignore data enter
                            }
                        }

                        //TODO verify the number of apData                  
                        string[] chunkChecker = dataChunk.Split(',');
                        {
                            StringBuilder tempBuilder;
                            tempBuilder = new StringBuilder();
                            tempBuilder.Clear();
                            tempBuilder.AppendLine(dataChunk);
                            File.AppendAllText(Definitions.TempFileName, tempBuilder.ToString());
                        }
                        dataChunk = string.Empty;
                        // Debug.WriteLine("[Success]Writing temp CSV file");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("[Error]Fail Writing CSV file > " + e.Message);
                }
            }
            else
            {
                //kalau recording stop time step harus stop juga
                if (isTimeStepping)
                {
                    isTimeStepping = !isTimeStepping;
                    timerStep.Stop();
                    timerStep.Reset();
                }
            }
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                ButtonUp_Click(this, null);
            }
            else if (e.Key == Key.PageDown)
            {
                ButtonDown_Click(this, null);
            }
            else if (e.Key == Key.B)
            {
                isRecording = !isRecording;
            }
        }
        private void AssessmentListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.PageUp)
            {
                ButtonUp_Click(this, null);
            }
            else if (e.Key == Key.PageDown)
            {
                ButtonDown_Click(this, null);
            }
        }
        private void selectAssessment_Click(object sender, RoutedEventArgs e)
        {
            ButtonDown_Click(this, null);
        }
        private void labeValue0_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool dataComplete = true;



            int activeItemNums = activeAssessment.AssociatedItemList.Count;

            if (functionMode == Definitions.FunctionMode.Training)
            {
                for (int ain = 0; ain < activeItemNums; ain++)
                {
                    labellingCombos[ain].SelectedIndex = labellingCombos[0].SelectedIndex;

                }
            }

            for (int ain = 0; ain < activeItemNums; ain++)
            {
                if (labellingCombos[ain].SelectedIndex == -1)
                {
                    dataComplete = false;
                    break;
                }
            }

            isLabellingFinish = dataComplete;
            if (isLabellingFinish) labelSaveButton.Visibility = Visibility.Visible;
        }
        #endregion

        #region Windows Control
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }

            this.KeyDown += new KeyEventHandler(KeyPressed);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            gloveSerial.ClosePort();
        }

        private void ClearElements()
        {

        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            functionMode = Definitions.FunctionMode.Training;
            UpdateWindow(functionMode);
        }

        private void Score_Click(object sender, RoutedEventArgs e)
        {
            functionMode = Definitions.FunctionMode.Classify;
            UpdateWindow(functionMode);
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            functionMode = Definitions.FunctionMode.Report;
            UpdateWindow(functionMode);
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            functionMode = Definitions.FunctionMode.Setting;
            UpdateWindow(functionMode);
        }

        private void UpdateWindow(Definitions.FunctionMode fm)
        {
            ContentGrid.Visibility = Visibility.Collapsed;
            ReportGrid.Visibility = Visibility.Collapsed;
            SettingGrid.Visibility = Visibility.Collapsed;

            switch (fm)
            {
                case Definitions.FunctionMode.Training:
                    ContentGrid.Visibility = Visibility.Visible;

                    break;
                case Definitions.FunctionMode.Classify:
                    ContentGrid.Visibility = Visibility.Visible;

                    break;
                case Definitions.FunctionMode.Report:
                    ReportGrid.Visibility = Visibility.Visible;

                    break;
                case Definitions.FunctionMode.Setting:
                    SettingGrid.Visibility = Visibility.Visible;

                    break;
            }
        }

        private void BigVideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            BigVideoPlayer.Position = TimeSpan.FromSeconds(0);
            BigVideoPlayer.Play();
        }

        private void SmallVideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            SmallVideoPlayer.Position = TimeSpan.FromSeconds(0);
            SmallVideoPlayer.Play();
        }

        #endregion

        #region Finite State
        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            var curTrainingState = trainingState;
            if (functionMode == Definitions.FunctionMode.Training)
            {
                switch (trainingState)
                {
                    case Definitions.TrainingState.Video:
                        //illegal
                        break;
                    case Definitions.TrainingState.Idle:
                        trainingState = Definitions.TrainingState.Video;
                        break;
                    case Definitions.TrainingState.Recording:
                        currentTrimmingId++;
                        repetitionText.Text = "Repetition " + currentTrimmingId;
                        //illegal
                        break;
                    case Definitions.TrainingState.Hold:
                        trainingState = Definitions.TrainingState.Idle;
                        break;
                        //stop emul at labeling
                }
                if (trainingState != curTrainingState)
                    UpdateContent(trainingState);
            }
            else if (functionMode == Definitions.FunctionMode.Classify)
            {
                //TODO add classify switch
                UpdateContent(classifyingState);
            }
        }
        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            var curTrainingState = trainingState;
            if (functionMode == Definitions.FunctionMode.Training)
            {
                switch (trainingState)
                {
                    case Definitions.TrainingState.Video:
                        trainingState = Definitions.TrainingState.Idle;
                        break;
                    case Definitions.TrainingState.Idle:
                        trainingState = Definitions.TrainingState.Recording;
                        break;
                    case Definitions.TrainingState.Recording:
                        trainingState = Definitions.TrainingState.Hold;
                        break;
                    case Definitions.TrainingState.Hold:
                        trainingState = Definitions.TrainingState.Labelling;
                        break;
                    case Definitions.TrainingState.Labelling:
                        if (isLabellingFinish)
                        {
                            trainingState = Definitions.TrainingState.Confirmation;
                        }
                        break;
                    case Definitions.TrainingState.Confirmation:
                        trainingState = Definitions.TrainingState.Video;
                        break;
                }
                if (trainingState != curTrainingState)
                    UpdateContent(trainingState);
            }
            else if (functionMode == Definitions.FunctionMode.Classify)
            {
                //TODO add classify switch
                UpdateContent(classifyingState);
            }
        }
        private void UpdateContent(Definitions.TrainingState ts)
        {
            FuncText.Text = "Tr :" + activeAssessment.AssessmentCode.ToString();
            StateText.Text = ts.ToString();
            switch (ts)
            {
                case Definitions.TrainingState.Video:
                    #region video state proccess
                    repetitionText.Visibility = Visibility.Hidden;

                    popUpBar.Visibility = Visibility.Hidden;
                    selectAssessment.Visibility = Visibility.Visible;

                    attentionText.Visibility = Visibility.Hidden;
                    confirmationText.Visibility = Visibility.Hidden;

                    AssessmentListView.Visibility = Visibility.Visible;
                    KinectPlayer.Visibility = Visibility.Hidden;

                    SmallVideoPlayer.Stop();
                    SmallVideoPlayer.Visibility = Visibility.Collapsed;
                    BigVideoPlayer.Visibility = Visibility.Visible;

                    try
                    {
                        BigVideoPlayer.Source = new Uri("data/video/" + activeAssessment.AssessmentCode.ToString() + ".mp4", UriKind.Relative);
                        BigVideoPlayer.Play();
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("[Error] " + exc.Message);
                    }
                    #endregion
                    break;

                case Definitions.TrainingState.Idle:
                    #region idle state proccess

                    InitTempData();

                    repetitionText.Visibility = Visibility.Hidden;
                    selectAssessment.Visibility = Visibility.Collapsed;
                    attentionText.Visibility = Visibility.Visible;
                    popUpText.Text = "Ready to start the assessment?";
                    popUpBar.Visibility = Visibility.Visible;
                    isRecording = false;
                    KinectPlayer.Visibility = Visibility.Visible;
                    AssessmentListView.Visibility = Visibility.Hidden;
                    SmallVideoPlayer.Visibility = Visibility.Visible;
                    BigVideoPlayer.Source = null;
                    BigVideoPlayer.Visibility = Visibility.Collapsed;

                    try
                    {
                        SmallVideoPlayer.Source = new Uri("data/video/" + activeAssessment.AssessmentCode.ToString() + ".mp4", UriKind.Relative);
                        SmallVideoPlayer.Play();
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("[Error] " + exc.Message);
                    }
                    #endregion
                    break;

                case Definitions.TrainingState.Recording:
                    #region recording state proccess
                    repetitionText.Visibility = Visibility.Visible;
                    currentTrimmingId = 0;
                    repetitionText.Text = "Repetition " + currentTrimmingId;
                    popUpBar.Visibility = Visibility.Hidden;
                    isRecording = true;
                    #endregion
                    break;

                case Definitions.TrainingState.Hold:
                    #region hold state proccess
                    repetitionText.Visibility = Visibility.Hidden;
                    popUpText.Text = "Save this assessment session?";
                    popUpBar.Visibility = Visibility.Visible;
                    isRecording = false;
                    #endregion

                    break;

                case Definitions.TrainingState.Labelling:


                    #region labelling state proccess
                    attentionText.Visibility = Visibility.Hidden;
                    popUpBar.Visibility = Visibility.Hidden;
                    labellingPanel.Visibility = Visibility.Visible;
                    isLabellingFinish = false;

                    foreach (Grid lg in labellingGrids)
                    {
                        lg.Visibility = Visibility.Collapsed;
                    }
                    labelSaveButton.Visibility = Visibility.Hidden;

                    //reset labelling element
                    int activeItemNums = activeAssessment.AssociatedItemList.Count;
                    for (int ain = 0; ain < activeItemNums; ain++)
                    {
                        labellingGrids[ain].Visibility = Visibility.Visible;
                        labellingText[ain].Text = activeAssessment.AssociatedItemList[ain].ItemCode.ToString();
                    }

                    foreach (ComboBox cb in labellingCombos)
                    {
                        cb.SelectedIndex = -1;
                    }
                    #endregion

                    break;

                case Definitions.TrainingState.Confirmation:

                    labellingPanel.Visibility = Visibility.Hidden;
                    confirmationText.Visibility = Visibility.Visible;

                    #region store data to raw trainning
                    int fileNum = 1;
                    string isoFileLocRaw = "Training/Raw/" + activeAssessment.AssessmentCode.ToString() + "/" + activeUser.ToString();
                    string isoFileConfRaw = isoFileLocRaw + "/config.txt";

                    try
                    {
                        if (isoStore.FileExists(isoFileConfRaw))
                        {
                            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(isoFileConfRaw, FileMode.Open, isoStore))
                            {
                                using (StreamReader reader = new StreamReader(isoStream))
                                {
                                    string sNM = reader.ReadToEnd().Split('.')[0];
                                    fileNum = Convert.ToInt32(sNM);
                                }
                            }
                            fileNum++;
                        }
                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(isoFileConfRaw, FileMode.Create, isoStore))
                        {
                            using (StreamWriter writer = new StreamWriter(isoStream))
                            {
                                writer.WriteLine(fileNum.ToString() + ".");
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("[Error]Fail modifying config file on " + isoFileLocRaw + " > " + exc.Message.ToString());
                    }


                    //generate raw file name
                    string destinationFileName = fileNum.ToString() + "_" + activeSide.ToString();

                    int labelIndex = 0;
                    tempScoreLabel = new string[2][];
                    tempScoreLabel[0] = new string[activeAssessment.AssociatedItemList.Count];
                    tempScoreLabel[1] = new string[activeAssessment.AssociatedItemList.Count];
                    foreach (ComboBox cb in labellingCombos)
                    {
                        if (cb.SelectedIndex != -1)
                        {
                            destinationFileName += ("_" + cb.SelectedIndex.ToString());
                            tempScoreLabel[0][labelIndex] = activeAssessment.AssociatedItemList[labelIndex].ItemCode.ToString();
                            tempScoreLabel[1][labelIndex] = labellingCombos[labelIndex].SelectedValue.ToString();

                        }
                        labelIndex++;
                    }

                    destinationFileName += ".csv";

                    //Copying temp file to raw dictionary
                    try
                    {
                        using (FileStream tempStream = new FileStream("data/tempdata.csv", FileMode.Open))
                        {
                            using (StreamReader tempReader = new StreamReader(tempStream))
                            {
                                //create file dengan ID trial sekian, jika file sudah ada (angka di config di reset) akan dibuat folder baru
                                using (IsolatedStorageFileStream destinationStream = new IsolatedStorageFileStream(isoFileLocRaw + "/" + destinationFileName, FileMode.Create))
                                {
                                    using (StreamWriter destinationWriter = new StreamWriter(destinationStream))
                                    {
                                        destinationWriter.Write(tempReader.ReadToEnd());
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("[Error] Fail managing config file" + exc.Message);
                    }
                    #endregion

                    #region load data from raw trainning
                    string rawPath = isoFileLocRaw + "/" + destinationFileName;
                    string[][] rawData = null;
                    string[][] preprocData = null;
                    if (isoStore.FileExists(rawPath))
                    {
                        try
                        {
                            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(rawPath, FileMode.Open, isoStore))
                            {
                                using (StreamReader reader = new StreamReader(isoStream))
                                {
                                    string[] rd = reader.ReadToEnd().Split('\n');
                                    rawData = new string[rd.Length - 1][];

                                    int i = 0;
                                    foreach (string sd in rd)
                                    {
                                        if (i == rd.Length - 1) break; //exclude last line of raw file
                                        rawData[i] = (sd.Split('\r')[0]).Split(',');
                                        i++;
                                    }
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine("[Error]Fail loading raw data >" + exc.Message);
                        }

                    }
                    #endregion

                    #region save data to preproc
                    foreach (Item assItem in activeAssessment.AssociatedItemList)
                    {
                        //HACK Only save destinated item
                        switch (assItem.ItemCode)
                        {
                            case Definitions.ItemCode.U2A:
                            case Definitions.ItemCode.U3B:
                            case Definitions.ItemCode.U4C:
                            case Definitions.ItemCode.U5B:
                            case Definitions.ItemCode.U7B:
                            case Definitions.ItemCode.U8B:
                            case Definitions.ItemCode.U8C:
                                try
                                {
                                    Preprocessor preprocessor = new Auros.Preprocessor();
                                    preprocData = preprocessor.Preprocess(rawData, activeAssessment, activeSide, assItem);

                                    //generate raw file name pake filenum dilabelling
                                    //iterate through old file name
                                    int itemSearchIndex = 0;
                                    foreach (string tLabel in tempScoreLabel[0])
                                    {
                                        if (tLabel == assItem.ItemCode.ToString())
                                        {
                                            break;
                                        }
                                        itemSearchIndex++;
                                    }

                                    destinationFileName = "Training/Preproc/" + assItem.ItemCode.ToString() + "/" + activeUser.ToString() + "/" + fileNum.ToString() + "_" + activeSide.ToString() + "_" + tempScoreLabel[1][itemSearchIndex].ToString() + ".csv";

                                    //write array to dir
                                    using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(destinationFileName, FileMode.Create, isoStore))
                                    {
                                        using (StreamWriter writer = new StreamWriter(isoStream))
                                        {
                                            foreach (string[] line in preprocData)
                                            {
                                                string lineChunk = string.Empty;
                                                foreach (string l in line)
                                                {
                                                    if (lineChunk != string.Empty) lineChunk += ",";
                                                    lineChunk += l;
                                                }
                                                writer.WriteLine(lineChunk);
                                            }
                                        }
                                    }

                                    //save data
                                }
                                catch (Exception exc)
                                {
                                    Debug.WriteLine("[Error]Fail writing to preproc dir >" + exc.Message);
                                }

                                break;
                        }

                    }
                    #endregion

                    //TODO create master data


                    break;
            }
        }
        private void UpdateContent(Definitions.ClassifyingState cs)
        {
            FuncText.Text = "Cl" + activeAssessment.AssessmentCode.ToString();
            StateText.Text = cs.ToString();
            throw new NotImplementedException();
        }


        #endregion

        private void SideComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //activeSide = (Definitions.AssessSide)SideComboBox.SelectedIndex;
        }
    }
}
