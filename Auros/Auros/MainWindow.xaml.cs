using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;
using System.IO.IsolatedStorage;

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

        //class def
        JointManager jointManager;
        Serial gloveSerial;
        StringBuilder csvBuilder;
        public List<Assessment> assessmentLibrary { get; set; }
        Assessment activeAssessment;
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

        public MainWindow()
        {
            InitKinect();

            ClearElements();
            gloveSerial = new Serial();
            gloveSerial.OpenPort(0);
            jointManager = new JointManager();
            csvBuilder = new StringBuilder();

            activeUser = Definitions.UserCode.Therapist;
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

            functionMode = Definitions.FunctionMode.Training;
            trainingState = Definitions.TrainingState.Video;
            classifyingState = Definitions.ClassifyingState.Video;

            InitView();

            //HACK Emergency Test Here
        }

        private void InitView()
        {
            AssessmentListView.DataContext = this;
            AssessmentListView.SelectedIndex = 0;
            labellingComboBox.ItemsSource = Definitions.FMALabel;
            SettingGrid.Visibility = Visibility.Collapsed;
            ReportGrid.Visibility = Visibility.Collapsed;
            activeAssessment = (Assessment)AssessmentListView.SelectedItem;
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
                    //TODO count available assessment data for the current user here

                    foreach (Item itm in ass.AssociatedItemList)
                    {
                        string itmDir = FirstLevelDir + "/Preproc/" + itm.ItemCode.ToString() + "/" + activeUser.ToString();
                        if (!isoStore.DirectoryExists(itmDir))
                            isoStore.CreateDirectory(itmDir);
                        itm.PreProcDataPath = itmDir;
                        //TODO count available preproc item data for the current user here
                    }
                }
                Debug.WriteLine("[Success]Initializing folder");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Initializing folder >" + e.Message);
            }
        }

        #region Assessment Object Control
        private void InitAssessmentLibrary()
        {
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
                        if (itm.Length == 3 && j != 0)
                        {
                            Item newItem = new Item();
                            newItem.ItemCode = (Definitions.ItemCode)Enum.Parse(typeof(Definitions.ItemCode), itm);
                            ass.AssociatedItemList.Add(newItem);
                        }
                        j++;
                    }
                    i++;
                }
                Debug.WriteLine("[Success]Load Item object to assessment library");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Load Item object to assessment library >" + e.Message);
            }

        }

        private void AssessmentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                File.Delete(Definitions.TempFileName);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Cant delete temp file" + exc.Message);
            }
            activeAssessment = (Assessment)AssessmentListView.SelectedItem;
            string[] rawFilter = File.ReadAllLines(Definitions.FeaturedDataEachAssessmentPath);
            string[][] dataFilter = new string[rawFilter.Length][];
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
                csvBuilder.AppendLine(dataHeader);
                File.AppendAllText(Definitions.TempFileName, csvBuilder.ToString());
                Debug.WriteLine("[Success]Creating Filter and temporary data ");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Creating Filter and temporary data >" + exc.Message);
            }            
        }

        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ScoreCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        #endregion

        #region Data Control
        private void FetchSensorData(IReadOnlyDictionary<JointType, Joint> fJoints)
        {
            string dataChunk = "";
            if (isRecording)
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
                    string[] gloveSensorData = gloveSensorDataRaw.Split('#');

                    if (gloveSensorData.Length == 8 && fJoints != null) //data validation
                    {
                        if (isTimeStepping)
                        {
                            isTimeStepping = !isTimeStepping;
                            timerStep.Start();
                        }
  
                        dataChunk += timerStep.ElapsedMilliseconds.ToString();
                        for(int i=2;i<activeFilter[1].Length;i++)
                        {
                            string apData = "";
                            if (activeFilter[1][i] == "1")
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
                                        apData = gloveSensorData[2] + "*" + gloveSensorData[3] + "*" + gloveSensorData[4];
                                        break;
                                    case "Gyro":
                                        apData = gloveSensorData[5] + "*" + gloveSensorData[6] + "*" + gloveSensorData[7];
                                        break;
                                }

                                //TODO iterate through joint type and append data if exist
                                if (apData == "")
                                {
                                    JointType selJoint;
                                    Enum.TryParse(activeFilter[0][i], out selJoint);
                                    apData += (fJoints[selJoint].Position.X.ToString() + "*" + fJoints[selJoint].Position.Y.ToString() + "*" + fJoints[selJoint].Position.Z.ToString());
                                    if (fJoints[selJoint].Position.X.ToString() == ""|| fJoints[selJoint].Position.Y.ToString() == "" || fJoints[selJoint].Position.Z.ToString() == "")
                                    {
                                        int hsaas = 9;
                                    }
                                }
                                dataChunk += ("," + apData);                               
                            }
                        }

                        csvBuilder = new StringBuilder();
                        csvBuilder.AppendLine(dataChunk);
                        File.AppendAllText(Definitions.TempFileName, csvBuilder.ToString());
                        dataChunk = "";
                        //Debug.WriteLine("[Success]Writing CSV file");
                    }
                    else
                    {
                        Debug.WriteLine("[Error]Glove data integrity error");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("[Error]Fail Writing CSV file > " + e.Message);
                }
            }
            else
            {
                if (!isTimeStepping) isTimeStepping = !isTimeStepping;
            }
        }
        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                ButtonUp_Click(this, null);
            }
            else if (e.Key == Key.Down)
            {
                ButtonDown_Click(this, null);
            }
            else if (e.Key == Key.Enter)
            {
                isRecording = !isRecording;
            }
        }
        private void UpdateContent(Definitions.TrainingState ts)
        {
            throw new NotImplementedException();
        }
        private void UpdateContent(Definitions.ClassifyingState cs)
        {
            throw new NotImplementedException();
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
                    //HACK Clear data buffer, Handle body lebih dari satu

                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
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


        #endregion

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            //TODO do this whole button click shit
            if (functionMode == Definitions.FunctionMode.Training)
            {
                switch (trainingState)
                {

                    case Definitions.TrainingState.Video:
                        break;
                    case Definitions.TrainingState.Idle:
                        trainingState = Definitions.TrainingState.Video;
                        break;
                    case Definitions.TrainingState.Recording:
                        trainingState = Definitions.TrainingState.Idle;
                        break;
                    case Definitions.TrainingState.Hold:
                        trainingState = Definitions.TrainingState.Idle;
                        break;
                    case Definitions.TrainingState.Labelling:
                        break;
                    case Definitions.TrainingState.Confirmation:
                        trainingState = Definitions.TrainingState.Idle;
                        break;
                }

                UpdateContent(trainingState);
            }
            else if (functionMode == Definitions.FunctionMode.Classify)
            {
                UpdateContent(classifyingState);
            }
        }
        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
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
                        trainingState = Definitions.TrainingState.Confirmation;
                        break;
                    case Definitions.TrainingState.Confirmation:
                        trainingState = Definitions.TrainingState.Video;
                        break;
                }
                UpdateContent(trainingState);
            }
            else if (functionMode == Definitions.FunctionMode.Classify)
            {
                UpdateContent(classifyingState);
            }
        }


    }
}
