//*********************************************************
//
// (c) Copyright 2014 Dr. Thomas Fernandez
// 
// All rights reserved.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfTransducer
{
    class ArtEvaluator : EvaluatorForDoubles
    {

        static public BitmapImage targetBitmap;
        static public RenderTargetBitmap genBitmap=null;
        Random rand = new Random();

        byte[] targetPixelArray=null;
        byte[] genPixelArray=null;

        int pixelArrayStride;

        int sampleSize = 1000;

        Canvas genCanvas = new Canvas();

        public ArtEvaluator()
        {
            targetBitmap = new BitmapImage(new Uri(@"..\..\..\..\ML01.jpg",UriKind.Relative));

            GetByteArrayFromBitmap(targetBitmap,ref targetPixelArray);

            pixelArrayStride = targetBitmap.PixelWidth * 4;

            genBitmap = new RenderTargetBitmap(
                    (int)(targetBitmap.Width * 1.0),
                    (int)(targetBitmap.Height * 1.0),
                    96,
                    96,
                    PixelFormats.Pbgra32);

        }

        public string loadTargetBitmap(String loc){
            targetPixelArray = null;
            try
            {
                targetBitmap = new BitmapImage(new Uri(loc, UriKind.Relative));

                GetByteArrayFromBitmap(targetBitmap, ref targetPixelArray);

                pixelArrayStride = targetBitmap.PixelWidth * 4;
                return "Target image loaded successfully";
            }
            catch (Exception e) {
                return "Failed to load target image. Exception: " + e.ToString();
            }
        }

        public double evaluate(List<double> solution)
        {
            GenotypeToPhenotype(solution);
            double fitness = FitnessFunction();
            return fitness;
        }


        void getColorFromPixelArray(byte[] pixelArray, int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            int index = 0;
            //int index = y * pixelArrayStride + 4 * x;
            //int index = y * x * 4;
            if (x == 0 && y != 0) { index = y * 4; }
            else if (x != 0 && y == 0) { index = x * 10 * 4; }
            else { index = x * 10 + y * 4; }
            r = pixelArray[index];
            g = pixelArray[index + 1];
            b = pixelArray[index + 2];
            a = pixelArray[index + 3];
        }


        private void GetByteArrayFromBitmap(BitmapSource b, ref byte[] bArray)
        {
            int size; 
            if (bArray == null)
            {
                pixelArrayStride = b.PixelWidth * 4;
                size = b.PixelHeight * pixelArrayStride;
                bArray = new byte[size];
            }
            b.CopyPixels(bArray, pixelArrayStride, 0);
        }


        static public double clamp(double v, double min, double max)
        {
            if (v < min) v = min;
            if (v > max) v = max;
            return v;
        }

        public Canvas GenotypeToPhenotypeShrinkingCircles2(List<double> solution)
        {
            // Make a new canvas of the same size as the target bitmap.
            if (genCanvas == null)
            {
                genCanvas = new Canvas();
                genCanvas.Width = targetBitmap.Width;
                genCanvas.Height = targetBitmap.Height;
            }

            genCanvas.Children.Clear();

            //The sizeFactor allows the phenotype to implement smaller and smaller ellipses as it implements the list of ellipses.
            double sizeFactor = solution.Count() / 400;


            // Here we loop through all the doubles in the solution 8 at a time.
            // Each time through the loop creates another ellipse.
            // The number of ellipses is controlled by the size of the solutions as set near line 
            for (int i = 0; i <= solution.Count - 8; i += 8)
            {
                // In this case we are using 8 doubles for each ellipse.
                // The position of the ellipse is at x,y.
                // The Height and Width is h and w.
                // The color is determined by the a,r,g and b
                double x = solution[i + 0];
                double y = solution[i + 1];
                double h = solution[i + 2];
                double w = solution[i + 3];
                double r = solution[i + 4];
                double g = solution[i + 5];
                double b = solution[i + 6];
                double a = solution[i + 7];


                // Normalize the values for position ,height and width.
                //x = (genCanvas.Width * 2 * x) - genCanvas.Width;
                //y = (genCanvas.Height * 2 * y) - genCanvas.Height;
                //h = clamp((genCanvas.Height * 0.5 * h), 0.0, genCanvas.Height);
                //w = clamp((genCanvas.Height * 0.5 * w), 0.0, genCanvas.Height); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?




                x = (genCanvas.Width * 1.2 * x) - (genCanvas.Width * 0.2);
                y = (genCanvas.Height * 1.2 * y) - (genCanvas.Height * 0.2);
                h = clamp((genCanvas.Height * 0.2 * sizeFactor * h), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor);
                w = clamp((genCanvas.Height * 0.2 * sizeFactor * w), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?


                sizeFactor *= 0.99;



                // Normalize and clamp values for colors.
                byte rb = Convert.ToByte(clamp(r * 256.0, 0, 255));
                byte gb = Convert.ToByte(clamp(g * 256.0, 0, 255));
                byte bb = Convert.ToByte(clamp(b * 256.0, 0, 255));
                byte ab = Convert.ToByte(clamp(a * 256.0, 0, 255));


                // Create an ellipse.
                Ellipse ellipse = new Ellipse();

                // This allows positioning the ellipse without the need for TranslationTransforms
                Canvas.SetLeft(ellipse, x);
                Canvas.SetTop(ellipse, y);

                // Set the size of the ellipse and the color.
                ellipse.Height = h;
                ellipse.Width = w;
                ellipse.Fill = new SolidColorBrush(Color.FromArgb(ab, rb, gb, bb));

                // Add it to the canvas.
                genCanvas.Children.Add(ellipse);
            }

            return genCanvas;
        }


        private double FitnessFuntionSidekick(int x,int y)
        {
            double fitness = 0.0;
            int rx = x; int ry = y;
            // Get the random sample point
             //rx = rand.Next(0, (int)targetBitmap.Width);
             //ry = rand.Next(0, (int)targetBitmap.Height);

            //Target Color Values
            byte tRed;
            byte tGreen;
            byte tBlue;
            byte tAlpha;

            //Generated Color Values
            byte gRed;
            byte gGreen;
            byte gBlue;
            byte gAlpha;

            // Get the Target color values
            getColorFromPixelArray(targetPixelArray, rx, ry, out tRed, out tGreen, out tBlue, out tAlpha);

            //Get the Generated color values
            getColorFromPixelArray(genPixelArray, rx, ry, out gRed, out gGreen, out gBlue, out gAlpha);

            //Ge the color differences
            double RedError = Math.Abs(tRed - gRed);
            double GreenError = Math.Abs(tGreen - gGreen);
            double BlueError = Math.Abs(tBlue - gBlue);

            ////square each of the color differences (optional but probably a good idea, as mentioned in class on July 9th 2014)
            RedError *= RedError;
            GreenError *= GreenError;
            BlueError *= BlueError;

            //get the total error for the current pixel (rx,ry).
            double pixelError = (RedError + GreenError + BlueError) / 256.0;

            //square the total difference 

            pixelError *= pixelError;



            //add that to all the other pixel errors.
            fitness += pixelError;
            return fitness;
        }

        Boolean isTargetBitmapEven;
        double fitness = 0;
        Boolean s1Done, s2Done, s3Done, s4Done; 
            

        private double FitnessFunction()
        {
            
            //Convert the genCanvas to the genBitmap
            genBitmap.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
            {
                ConvertFEtoRTB(genCanvas, ref genBitmap);

                //Convert the genBitmap to the genPixelArray
                GetByteArrayFromBitmap(genBitmap, ref genPixelArray);
            }));
            Range<double> sectionOneParentWidth, sectionTwoParentWidth, sectionThreeParentWidth, sectionFourParentWidth;
            sectionOneParentWidth = new Range<double>(); sectionTwoParentWidth = new Range<double>(); sectionThreeParentWidth = new Range<double>(); sectionFourParentWidth = new Range<double>();
            Range<double> sectionOneParentHeight, sectionTwoParentHeight, sectionThreeParentHeight, sectionFourParentHeight;
            sectionOneParentHeight = new Range<double>(); sectionTwoParentHeight = new Range<double>(); sectionThreeParentHeight = new Range<double>(); sectionFourParentHeight = new Range<double>();
            fitness = 0;
            s1Done = s2Done = s3Done = s4Done = false;
            //List<Range<double>> sectionWidths = new List<Range<double>>();
           // List<Range<double>> sectionHeights = new List<Range<double>>();

           targetBitmap.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
           {
            if ((targetBitmap.Height * targetBitmap.Width % 2) == 0 ) //If the image has an even number of pixels, split the iamge up into 4 pieces
            {
                sectionOneParentWidth.Minimum = 0; sectionOneParentWidth.Maximum = (targetBitmap.Width / 2);
                sectionOneParentHeight.Minimum = 0; sectionOneParentHeight.Maximum = (targetBitmap.Height / 2);

                sectionTwoParentWidth.Minimum = (targetBitmap.Width / 2); sectionTwoParentWidth.Maximum = targetBitmap.Width;
                sectionTwoParentHeight.Minimum = 0; sectionTwoParentHeight.Maximum = (targetBitmap.Height / 2);

                sectionThreeParentWidth.Minimum = 0; sectionThreeParentWidth.Maximum = (targetBitmap.Width / 2);
                sectionThreeParentHeight.Minimum = (targetBitmap.Height / 2); sectionThreeParentHeight.Maximum = (targetBitmap.Height);

                sectionFourParentWidth.Minimum = (targetBitmap.Width / 2); sectionFourParentWidth.Maximum = targetBitmap.Width;
                sectionFourParentHeight.Minimum = (targetBitmap.Height / 2); sectionFourParentHeight.Maximum = (targetBitmap.Height);
                sampleSize = (int)((targetBitmap.Height/2) * (targetBitmap.Width / 2));
                isTargetBitmapEven = true;
            }
            else {  //If the image has an odd number of pixels split, the image up into 3 pieces

                sectionOneParentWidth.Minimum = 0; sectionOneParentWidth.Maximum = targetBitmap.Width;
                sectionOneParentHeight.Minimum = 0; sectionOneParentHeight.Maximum = (targetBitmap.Height / 3);

                sectionTwoParentWidth.Minimum = 0; sectionTwoParentWidth.Maximum = targetBitmap.Width;
                sectionTwoParentHeight.Minimum = (targetBitmap.Height / 3); sectionTwoParentHeight.Maximum = ((2 * targetBitmap.Height) / 3);

                sectionThreeParentWidth.Minimum = 0; sectionThreeParentWidth.Maximum = targetBitmap.Width;
                sectionThreeParentHeight.Minimum = ((2 * targetBitmap.Height) / 3); sectionThreeParentHeight.Maximum = targetBitmap.Height;
                sampleSize = (int)(targetBitmap.Height * targetBitmap.Width);
                isTargetBitmapEven = false;
            }
           }));
            //Parent Section 1 - First Thread

            System.Threading.Thread thread1 = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){

                bitmapDivider(sectionOneParentWidth, sectionOneParentHeight);
                
                startNestThreads(sectionOneParentWidth,sectionOneParentHeight);
                s1Done = true;
            }));
            thread1.Start();

            //Section 2 - Second Thread
            System.Threading.Thread thread2 = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){

                bitmapDivider(sectionTwoParentWidth, sectionTwoParentHeight);

                startNestThreads(sectionTwoParentWidth, sectionTwoParentHeight);
                s2Done = true;

            }));

            thread2.Start();

            //Section 3 - Third Thread
            System.Threading.Thread thread3 = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){

                bitmapDivider(sectionThreeParentWidth, sectionThreeParentHeight);

                startNestThreads(sectionThreeParentWidth, sectionThreeParentHeight);
                s3Done = true;
            }));

            thread3.Start();

            //Section 4 - Fourth Thread if the image is even
            if (isTargetBitmapEven == true) 
            {
                System.Threading.Thread thread4 = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){

                    bitmapDivider(sectionFourParentWidth, sectionFourParentHeight);
                    startNestThreads(sectionFourParentWidth, sectionFourParentHeight);
                    s4Done = true;
                }));

                thread4.Start();
            }

            //Checker Thread

            if (isTargetBitmapEven == true)
            {
                while (s1Done == s2Done == s3Done == s4Done == false)
                {
                    //Keep Checking
                }
                if ((s1Done == s2Done == s3Done == s4Done == true)) //Redundent I know
                {
                    fitness /= sampleSize;

                }
            }
            else{
                while (s1Done == s2Done == s3Done == false)
                {
                    //Keep Checking
                }
                if ((s1Done == s2Done == s3Done == true)) //Redundent I know
                {
                    fitness /= sampleSize;
                }
            }
            return -fitness;
        }

        double minDividerWidth = 100;
        double minDividerHeight = 100;

        /// <summary>
        /// The recursive method used to divide each parent/child section into multiple subsections. For example, if you had a even bitmap it would be divided into 4 sections and each of those sections would be divided into four more sections; 
        /// and so on and so forth.
        /// </summary>
        /// <param name="sectionWidth">The Range of the width of the current parent section</param> 
        /// <param name="sectionHeight">The Range of the height of the current parent section</param>
        public void bitmapDivider(Range<double> sectionWidth, Range<double> sectionHeight)
        {
            double totalWidthOfCurrentSection = sectionWidth.Maximum - sectionWidth.Minimum; //gets the total width of the current section to divide
            double totalHeighOfCurrentSection = sectionHeight.Maximum - sectionHeight.Minimum; //gets the total height of the current section to divide
            if (isTargetBitmapEven)
            {
                if (totalWidthOfCurrentSection > minDividerWidth && totalHeighOfCurrentSection > minDividerHeight)
                {
                    Range<double> sectionOneChildWidth, sectionTwoChildWidth, sectionThreeChildWidth, sectionFourChildWidth;
                    sectionOneChildWidth = new Range<double>(); sectionTwoChildWidth = new Range<double>(); sectionThreeChildWidth = new Range<double>(); sectionFourChildWidth = new Range<double>();
                    Range<double> sectionOneChildHeight, sectionTwoChildHeight, sectionThreeChildHeight, sectionFourChildHeight;
                    sectionOneChildHeight = new Range<double>(); sectionTwoChildHeight = new Range<double>(); sectionThreeChildHeight = new Range<double>(); sectionFourChildHeight = new Range<double>();

                    sectionOneChildWidth.Minimum = sectionWidth.Minimum; sectionOneChildWidth.Maximum = sectionWidth.Minimum + (totalWidthOfCurrentSection / 2);
                    sectionOneChildHeight.Minimum = sectionHeight.Minimum; sectionOneChildHeight.Maximum = sectionHeight.Minimum + (totalHeighOfCurrentSection / 2);

                    sectionTwoChildWidth.Minimum = sectionWidth.Minimum + (totalWidthOfCurrentSection / 2); sectionTwoChildWidth.Maximum = sectionWidth.Maximum;
                    sectionTwoChildHeight.Minimum = sectionHeight.Minimum; sectionTwoChildHeight.Maximum = sectionHeight.Minimum + (totalHeighOfCurrentSection / 2);

                    sectionThreeChildWidth.Minimum = sectionWidth.Minimum; sectionThreeChildWidth.Maximum = sectionWidth.Minimum + (totalWidthOfCurrentSection / 2);
                    sectionThreeChildHeight.Minimum = sectionHeight.Minimum + (totalHeighOfCurrentSection / 2); sectionThreeChildHeight.Maximum = (sectionHeight.Maximum);

                    sectionFourChildWidth.Minimum = sectionWidth.Minimum + (totalWidthOfCurrentSection / 2); sectionFourChildWidth.Maximum = sectionWidth.Maximum;
                    sectionFourChildHeight.Minimum = sectionHeight.Minimum + (totalHeighOfCurrentSection / 2); sectionFourChildHeight.Maximum = (sectionHeight.Maximum);

                    sectionWidth.children = new List<Range<double>>() { };
                    sectionWidth.children.Add(sectionOneChildWidth);
                    sectionWidth.children.Add(sectionTwoChildWidth);
                    sectionWidth.children.Add(sectionThreeChildWidth);
                    sectionWidth.children.Add(sectionFourChildWidth);

                    sectionHeight.children = new List<Range<double>>() { };
                    sectionHeight.children.Add(sectionOneChildHeight);
                    sectionHeight.children.Add(sectionTwoChildHeight);
                    sectionHeight.children.Add(sectionThreeChildHeight);
                    sectionHeight.children.Add(sectionFourChildHeight);

                    if ((sectionOneChildWidth.Maximum - sectionOneChildWidth.Minimum) > minDividerWidth && (sectionOneChildHeight.Maximum - sectionOneChildHeight.Minimum) > minDividerWidth)
                    {
                        bitmapDivider(sectionOneChildWidth, sectionOneChildHeight);
                    }

                    if ((sectionTwoChildWidth.Maximum - sectionTwoChildWidth.Minimum) > minDividerWidth && (sectionTwoChildHeight.Maximum - sectionTwoChildHeight.Minimum) > minDividerWidth)
                    {
                        bitmapDivider(sectionTwoChildWidth, sectionTwoChildHeight);
                    }

                    if ((sectionThreeChildWidth.Maximum - sectionThreeChildWidth.Minimum) > minDividerWidth && (sectionThreeChildHeight.Maximum - sectionThreeChildHeight.Minimum) > minDividerWidth)
                    {
                        bitmapDivider(sectionThreeChildWidth, sectionThreeChildHeight);
                    }

                    if ((sectionFourChildWidth.Maximum - sectionFourChildWidth.Minimum) > minDividerWidth && (sectionFourChildHeight.Maximum - sectionFourChildHeight.Minimum) > minDividerWidth)
                    {
                        bitmapDivider(sectionFourChildWidth, sectionFourChildHeight);
                    }
                }
                else{return;}
            }
            else { //If the bitmap has an odd number of pixels

                if (totalWidthOfCurrentSection > minDividerWidth && totalHeighOfCurrentSection > minDividerHeight)
                {

                    Range<double> sectionOneChildWidth, sectionTwoChildWidth, sectionThreeChildWidth;
                    sectionOneChildWidth = new Range<double>(); sectionTwoChildWidth = new Range<double>(); sectionThreeChildWidth = new Range<double>();
                    Range<double> sectionOneChildHeight, sectionTwoChildHeight, sectionThreeChildHeight;
                    sectionOneChildHeight = new Range<double>(); sectionTwoChildHeight = new Range<double>(); sectionThreeChildHeight = new Range<double>();

                    sectionOneChildWidth.Minimum = sectionWidth.Minimum; sectionOneChildWidth.Maximum = sectionWidth.Maximum;
                    sectionOneChildHeight.Minimum = sectionHeight.Minimum; sectionOneChildHeight.Maximum = (sectionHeight.Maximum / 3);

                    sectionTwoChildWidth.Minimum = sectionWidth.Minimum; sectionTwoChildWidth.Maximum = sectionWidth.Maximum;
                    sectionTwoChildHeight.Minimum = (sectionHeight.Maximum / 3); sectionTwoChildHeight.Maximum = ((2 * sectionHeight.Maximum) / 3);

                    sectionThreeChildWidth.Minimum = sectionWidth.Minimum; sectionThreeChildWidth.Maximum = sectionWidth.Maximum;
                    sectionThreeChildHeight.Minimum = ((2 * sectionHeight.Maximum) / 3); sectionThreeChildHeight.Maximum = sectionHeight.Maximum;

                    sectionWidth.children.Add(sectionOneChildWidth);
                    sectionWidth.children.Add(sectionTwoChildWidth);
                    sectionWidth.children.Add(sectionThreeChildWidth);

                    sectionHeight.children.Add(sectionOneChildHeight);
                    sectionHeight.children.Add(sectionTwoChildHeight);
                    sectionHeight.children.Add(sectionThreeChildHeight);

                    bitmapDivider(sectionOneChildWidth, sectionOneChildHeight);
                    bitmapDivider(sectionTwoChildWidth, sectionTwoChildHeight);
                    bitmapDivider(sectionThreeChildWidth, sectionThreeChildHeight);
                }
                else { return; }
            }
        }

        /// <summary>
        /// The recursive method used to start a thread for each subsection of the image. For example if an image section is divided into 4 more sections, each of those sections will get a thread to preform work on.
        /// </summary>
        /// <param name="sectionWidth">The current parent width</param>
        /// <param name="sectionHeight">The current parent height</param>
        public void startNestThreads(Range<double> sectionWidth, Range<double> sectionHeight){

            //This is the utmost parent main thread
            System.Threading.Thread threadSection = new System.Threading.Thread(new System.Threading.ThreadStart(delegate()
            {
                if (sectionWidth.children != null && sectionHeight.children != null) //If the current parent section has subsections  
                {
                    startNestThreads(sectionWidth.children[0], sectionHeight.children[0]);
                    startNestThreads(sectionWidth.children[1], sectionHeight.children[1]);
                    startNestThreads(sectionWidth.children[2], sectionHeight.children[2]);
                    startNestThreads(sectionWidth.children[3], sectionHeight.children[3]);
                }
                else {  //Parent section no longer has anymore childern

                    for (int x1 = (int)sectionWidth.Minimum; x1 <= sectionWidth.Maximum; x1++)
                    {
                        for (int y1 = (int)sectionHeight.Minimum; y1 <= sectionHeight.Maximum; y1++)
                        {
                            fitness += FitnessFuntionSidekick(x1, y1);

                            if (isTargetBitmapEven == true) { y1 += 1; } //increatement by twos insted of one = Faster
                        }
                        if (isTargetBitmapEven == true) { x1 += 1; } //increatement by twos insted of one = Faster
                    }
                    
                    return; 
                }
               
            }));
            threadSection.Start();

            while (threadSection.IsAlive == true) { 
                //Dont Return from the thead until its complete
            }
        }

        public Canvas GenotypeToPhenotypeShrinkingCircles(List<double> solution)
        {
            // Make a new canvas of the same size as the target bitmap.
            if (genCanvas == null)
            {
                genCanvas = new Canvas();
                genCanvas.Width = targetBitmap.Width;
                genCanvas.Height = targetBitmap.Height;
            }

            genCanvas.Children.Clear();

            //The sizeFactor allows the phenotype to implement smaller and smaller ellipses as it implements the list of ellipses.
            double sizeFactor = solution.Count() / 400;


            // Here we loop through all the doubles in the solution 8 at a time.
            // Each time through the loop creates another ellipse.
            // The number of ellipses is controlled by the size of the solutions as set near line 
            for (int i = 0; i <= solution.Count - 8; i += 8)
            {
                // In this case we are using 8 doubles for each ellipse.
                // The position of the ellipse is at x,y.
                // The Height and Width is h and w.
                // The color is determined by the a,r,g and b
                double x = solution[i + 0];
                double y = solution[i + 1];
                double h = solution[i + 2];
                double w = solution[i + 3];
                double r = solution[i + 4];
                double g = solution[i + 5];
                double b = solution[i + 6];
                double a = solution[i + 7];


                // Normalize the values for position ,height and width.
                //x = (genCanvas.Width * 2 * x) - genCanvas.Width;
                //y = (genCanvas.Height * 2 * y) - genCanvas.Height;
                //h = clamp((genCanvas.Height * 0.5 * h), 0.0, genCanvas.Height);
                //w = clamp((genCanvas.Height * 0.5 * w), 0.0, genCanvas.Height); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?




                x = (genCanvas.Width * 1.2 * x) - (genCanvas.Width * 0.2);
                y = (genCanvas.Height * 1.2 * y) - (genCanvas.Height * 0.2);
                h = clamp((genCanvas.Height * 0.2 * sizeFactor * h), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor);
                w = clamp((genCanvas.Height * 0.2 * sizeFactor * w), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?


                sizeFactor *= 0.99;



                // Normalize and clamp values for colors.
                byte rb = Convert.ToByte(clamp(r * 256.0, 0, 255));
                byte gb = Convert.ToByte(clamp(g * 256.0, 0, 255));
                byte bb = Convert.ToByte(clamp(b * 256.0, 0, 255));
                byte ab = Convert.ToByte(clamp(a * 256.0, 0, 255));


                // Create an ellipse.
                Ellipse ellipse = new Ellipse();

                // This allows positioning the ellipse without the need for TranslationTransforms
                Canvas.SetLeft(ellipse, x);
                Canvas.SetTop(ellipse, y);

                // Set the size of the ellipse and the color.
                ellipse.Height = h;
                ellipse.Width = w;
                ellipse.Fill = new SolidColorBrush(Color.FromArgb(ab, rb, gb, bb));

                // Add it to the canvas.
                genCanvas.Children.Add(ellipse);
            }

            return genCanvas;
        }

        public Canvas GenotypeToPhenotype(List<double> solution)
        {
            genCanvas.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate(){
            // Make a new canvas of the same size as the target bitmap.
            if (genCanvas.Children.Count <= 0)
            {
                genCanvas = new Canvas();
                
                genCanvas.Width = targetBitmap.Width;
                genCanvas.Height = targetBitmap.Height;
                for (int i = 0; i < solution.Count() / 8; i++)
                {
                    /*Ellipse ellipse = new Ellipse();
                    ellipse.Fill = new SolidColorBrush();
                    genCanvas.Children.Add(ellipse);*/

                    Polygon polygon = new Polygon();
                    polygon.Fill = new SolidColorBrush();
                    genCanvas.Children.Add(polygon);
                }
                
            }

            //genCanvas.Children.Clear();

            //The sizeFactor allows the phenotype to implement smaller and smaller ellipses as it implements the list of ellipses.
            //double sizeFactor = solution.Count() / 400;
            double sizeFactor = 4.0;
            int circleCount = 0;

            int solutionIndex=0;
            foreach (UIElement uie in genCanvas.Children)
            {
                //Ellipse ellipse = uie as Ellipse;

                Polygon polly = uie as Polygon;

                // In this case we are using 8 doubles for each ellipse.
                // The position of the ellipse is at x,y.
                // The Height and Width is h and w.
                // The color is determined by the a,r,g and b
                double x = solution[solutionIndex + 0];
                double y = solution[solutionIndex + 1];
                double h = solution[solutionIndex + 2];
                double w = solution[solutionIndex + 3];
                double r = solution[solutionIndex + 4];
                double g = solution[solutionIndex + 5];
                double b = solution[solutionIndex + 6];
                double a = solution[solutionIndex + 7];


                solutionIndex += 8;

                // Normalize the values for position ,height and width.
                //x = (genCanvas.Width * 2 * x) - genCanvas.Width;
                //y = (genCanvas.Height * 2 * y) - genCanvas.Height;
                //h = clamp((genCanvas.Height * 0.5 * h), 0.0, genCanvas.Height);
                //w = clamp((genCanvas.Height * 0.5 * w), 0.0, genCanvas.Height); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?




                x = (genCanvas.Width * 1.2 * x) - (genCanvas.Width * 0.2);
                y = (genCanvas.Height * 1.2 * y) - (genCanvas.Height * 0.2);
                h = clamp((genCanvas.Height * 0.2 * sizeFactor * h), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor);
                w = clamp((genCanvas.Height * 0.2 * sizeFactor * w), genCanvas.Height * 0.01, genCanvas.Height * 0.2 * sizeFactor); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?


                //sizeFactor *= 0.99;
                if (circleCount > 40) sizeFactor = 1.0;
                if (circleCount > 100) sizeFactor = 0.5;
                if (circleCount > 200) sizeFactor = 0.25;


                // Normalize and clamp values for colors.
                byte rb = Convert.ToByte(clamp(r * 256.0, 0, 255));
                byte gb = Convert.ToByte(clamp(g * 256.0, 0, 255));
                byte bb = Convert.ToByte(clamp(b * 256.0, 0, 255));
                byte ab = Convert.ToByte(clamp(a * 256.0, 0, 255));


 
                // This allows positioning the ellipse without the need for TranslationTransforms
                //Canvas.SetLeft(ellipse, x);
                //Canvas.SetTop(ellipse, y);

                Canvas.SetLeft(polly, x);
                Canvas.SetTop(polly, y);

                // Set the size of the ellipse and the color.
                //ellipse.Height = h;
                //ellipse.Width = w;

                /*PointCollection polygonPoints = new PointCollection();
                polygonPoints.Add(new Point(x, y + (2 * h / 2))); //Point for the top
                polygonPoints.Add(new Point(x + (w / 2), y));
                polygonPoints.Add(new Point(x, y - (2 * h / 2)));//Point for the bottom
                polygonPoints.Add(new Point(x - (w / 2), y)); */

                PointCollection polygonPoints = new PointCollection();
                polygonPoints.Add(new Point(x, y + (2 * h / 2))); 
                polygonPoints.Add(new Point(x + (w/6), y + (h/6)));
                polygonPoints.Add(new Point(x + (w / 2), y)); 
                polygonPoints.Add(new Point(x + (w / 6), y - (h/6)));
                polygonPoints.Add(new Point(x, y - (2 * h / 2))); 
                polygonPoints.Add(new Point(x - (w / 6), y - (h/6)));
                polygonPoints.Add(new Point(x - (w / 2), y)); 
                polygonPoints.Add(new Point(x - (w / 6), y + (h/6))); 

                polly.Points = polygonPoints;

                //ellipse.Fill = new SolidColorBrush(Color.FromArgb(ab, rb, gb, bb));

                //SolidColorBrush scb = ellipse.Fill as SolidColorBrush;
                //scb.Color = Color.FromArgb(ab, rb, gb, bb);

                SolidColorBrush scb2 = polly.Fill as SolidColorBrush;
                scb2.Color = Color.FromArgb(ab, rb, gb, bb);

                circleCount++;
            }
            }));
            return genCanvas;
            
        }


        static public Canvas GenotypeToPhenotypeSimple(List<double> solution)
        {
            // Make a new canvas of the same size as the target bitmap.
            Canvas genCanvas = new Canvas();
            genCanvas.Width = targetBitmap.Width;
            genCanvas.Height = targetBitmap.Height;


            // Here we loop through all the doubles in the solution 8 at a time.
            // Each time through the loop creates another ellipse.
            // The number of ellipses is controlled by the size of the solutions as set near line 
            for (int i = 0; i <= solution.Count - 8; i += 8)
            {
                // In this case we are using 8 doubles for each ellipse.
                // The position of the ellipse is at x,y.
                // The Height and Width is h and w.
                // The color is determined by the a,r,g and b
                double x = solution[i + 0];
                double y = solution[i + 1];
                double h = solution[i + 2];
                double w = solution[i + 3];
                double r = solution[i + 4];
                double g = solution[i + 5];
                double b = solution[i + 6];
                double a = solution[i + 7];


                // Normalize the values for position ,height and width.
                //x = (genCanvas.Width * 2 * x) - genCanvas.Width;
                //y = (genCanvas.Height * 2 * y) - genCanvas.Height;
                //h = clamp((genCanvas.Height * 0.5 * h), 0.0, genCanvas.Height);
                //w = clamp((genCanvas.Height * 0.5 * w), 0.0, genCanvas.Height); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?

                x = (genCanvas.Width * 1.2 * x) - (genCanvas.Width * 0.2);
                y = (genCanvas.Height * 1.2 * y) - (genCanvas.Height * 0.2);
                h = clamp((genCanvas.Height * 0.2 * h), genCanvas.Height * 0.01, genCanvas.Height * 0.2);
                w = clamp((genCanvas.Height * 0.2 * w), genCanvas.Height * 0.01, genCanvas.Height * 0.2); //I used genCanvas.Height instead of genCanvas.Width. Can anyone figure out why?


                // Normalize and clamp values for colors.
                byte rb = Convert.ToByte(clamp(r * 256.0, 0, 255));
                byte gb = Convert.ToByte(clamp(g * 256.0, 0, 255));
                byte bb = Convert.ToByte(clamp(b * 256.0, 0, 255));
                byte ab = Convert.ToByte(clamp(a * 256.0, 0, 255));


                // Create an ellipse.
                Ellipse ellipse = new Ellipse();

                // This allows positioning the ellipse without the need for TranslationTransforms
                Canvas.SetLeft(ellipse, x);
                Canvas.SetTop(ellipse, y);

                // Set the size of the ellipse and the color.
                ellipse.Height = h;
                ellipse.Width = w;
                ellipse.Fill = new SolidColorBrush(Color.FromArgb(ab, rb, gb, bb));

                // Add it to the canvas.
                genCanvas.Children.Add(ellipse);
            }

            return genCanvas;
        }



        public static void ConvertFEtoRTB(FrameworkElement visual, ref RenderTargetBitmap genBitmap)
        {
            double scaleFactor = 1.0;
            if (genBitmap == null)
            {
                genBitmap = new RenderTargetBitmap(
                    (int)(targetBitmap.Width * scaleFactor),
                    (int)(targetBitmap.Height * scaleFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);
            }
            genBitmap.Clear();
            Size size = new Size(targetBitmap.Width * scaleFactor, targetBitmap.Height * scaleFactor);

            visual.LayoutTransform = new ScaleTransform(scaleFactor, scaleFactor);

            visual.Measure(size);
            visual.Arrange(new Rect(size));

            genBitmap.Render(visual);
        }

        //static void ConvertFEtoRTB(FrameworkElement visual, ref RenderTargetBitmap genBitmap)
        //{
        //    double scaleFactor = 1.0;
        //    if (genBitmap == null)
        //    {
        //        genBitmap = new RenderTargetBitmap(
        //            (int)(visual.Width * scaleFactor),
        //            (int)(visual.Height * scaleFactor),
        //            96,
        //            96,
        //            PixelFormats.Pbgra32);
        //    }
        //    genBitmap.Clear();
        //    Size size = new Size(visual.Width * scaleFactor, visual.Height * scaleFactor);

        //    visual.LayoutTransform = new ScaleTransform(scaleFactor, scaleFactor);

        //    visual.Measure(size);
        //    visual.Arrange(new Rect(size));

        //    genBitmap.Render(visual);
        //}

        //static RenderTargetBitmap ConvertFEtoRTB(FrameworkElement visual)
        //{
        //    double scaleFactor = 1.0;
        //    RenderTargetBitmap bitmap = new RenderTargetBitmap(
        //        (int)(visual.ActualWidth * scaleFactor),
        //        (int)(visual.ActualHeight * scaleFactor),
        //        96,
        //        96,
        //        PixelFormats.Pbgra32);

        //    Size size = new Size(visual.ActualWidth * scaleFactor, visual.ActualHeight * scaleFactor);

        //    //visual.RenderTransform = new ScaleTransform(2.0, 2.0);
        //    visual.LayoutTransform = new ScaleTransform(scaleFactor, scaleFactor);

        //    visual.Measure(size);
        //    visual.Arrange(new Rect(size));

        //    bitmap.Render(visual);
        //    return bitmap;
        //}
    }
}
