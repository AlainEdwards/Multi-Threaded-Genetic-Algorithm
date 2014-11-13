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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Windows.Shapes;
using T_Objects;

namespace WpfTransducer
{
    class Functionality
    {

        public MainWindow mainWindow;
        public TargetWindow targetWindow;
        public TargetWindow genWindow;
        
        GeneticAlgorithm ga;
        ArtEvaluator sombrero = new ArtEvaluator();

        //TransformGroup canvasTransform;

        public Functionality()
        {
            
        }

        public void setup()
        {
            ga = new GeneticAlgorithm();
            // The population size is set to 1000 individuals of 1600 doubles each. 
            // Scince the doubles are used in groups of 8 to make ellipses this will result in 200 ellipses.
            ga.populate(500, 2400, 0.0, 1.0);

            // The target window displays the target image.
            targetWindow = new TargetWindow();
            targetWindow.Height = ArtEvaluator.targetBitmap.Height * 1.1;
            targetWindow.Width = ArtEvaluator.targetBitmap.Width * 1.1;
            targetWindow.Background = new ImageBrush(ArtEvaluator.targetBitmap);
            targetWindow.Show();

            // The target window displays the target image.
            genWindow = new TargetWindow();
            genWindow.Height = ArtEvaluator.targetBitmap.Height * 1.1;
            genWindow.Width = ArtEvaluator.targetBitmap.Width * 1.1;
            genWindow.Title = "GenWindow";
            genWindow.Show();

            //The main window is set to the same size as the target window
            mainWindow.Height = targetWindow.Height*3;
            mainWindow.Width = targetWindow.Width*3;

            mainWindow.progressCurrentIndividual.Minimum = 0;
            mainWindow.progressOverallIndividual.Minimum = 0;
            mainWindow.progressCurrentIndividual.Maximum = 100;
            mainWindow.progressOverallIndividual.Maximum = 100;

        }

        private System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        public string runGA(int count)
        {
            sw.Reset();
            sw.Start();

            System.Threading.Thread threadA = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){
                 mainWindow.progressOverallIndividual.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                 { mainWindow.progressOverallIndividual.Value = 0; }));
            }));
            threadA.Start();
            double progress = 0;
            for (int i = 0; i < count; i++)
            {
                
                System.Threading.Thread threadB = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){
                    mainWindow.progressCurrentIndividual.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    { mainWindow.progressCurrentIndividual.Value = 0; }));

                    mainWindow.labelIndividualNum.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    { mainWindow.labelIndividualNum.Content = "Current Individual Number: " + i.ToString(); }));

                    //mainWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    //{ mainWindow.InvalidateVisual(); }));
                }));
                threadB.Start();

                ga.scoreOfLastSolution = sombrero.evaluate(ga.solution); //Same as before

                progress = (Convert.ToDouble(i) / Convert.ToDouble(count)) * 100;
                System.Threading.Thread threadC = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){
                    mainWindow.progressCurrentIndividual.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    { mainWindow.progressCurrentIndividual.Value = 100; }));

                    mainWindow.labelNumOfCompIndividuals.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    { mainWindow.labelNumOfCompIndividuals.Content = "Total Individuals Completed " + i.ToString() ; }));

                    mainWindow.progressOverallIndividual.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
                    { mainWindow.progressOverallIndividual.Value = progress; }));

                    mainWindow.labelElapsedTime.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                    { mainWindow.labelElapsedTime.Content = "Elapsed Time (HH:MM:SS): " + sw.Elapsed.ToString(); }));
                }));
                threadC.Start();
            }
            if (sw.IsRunning) { sw.Stop(); }

            //System.Threading.Thread threadD = new System.Threading.Thread(new System.Threading.ThreadStart(delegate(){
            //    mainWindow.labelElapsedTime.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
            //        { mainWindow.labelElapsedTime.Content = "Elapsed Time (HH:MM:SS): " + sw.Elapsed.ToString(); }));
            //}));
            //threadD.Start();

            return ga.bestScoreSoFar.ToString();
        }

        public string loadImage(string loc) {
            string r = sombrero.loadTargetBitmap(loc);
            targetWindow.Height = ArtEvaluator.targetBitmap.Height * 1.1;
            targetWindow.Width = ArtEvaluator.targetBitmap.Width * 1.1;
            targetWindow.Background = new ImageBrush(ArtEvaluator.targetBitmap);
            genWindow.Height = ArtEvaluator.targetBitmap.Height * 1.1;
            genWindow.Width = ArtEvaluator.targetBitmap.Width * 1.1;
            genWindow.Title = "GenWindow";

            return r;
        }

        RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)(ArtEvaluator.targetBitmap.Width * 1.0),
                    (int)(ArtEvaluator.targetBitmap.Height * 1.0),
                    96,
                    96,
                    PixelFormats.Pbgra32);

        public string plotGA()
        {
            Canvas canvas = sombrero.GenotypeToPhenotype(ga.bestSolutionSoFar);

            //Rectangle rectangle = new Rectangle();
            //Canvas.SetLeft(rectangle, 0);
            //Canvas.SetTop(rectangle, 0);
            //rectangle.Width = ArtEvaluator.targetBitmap.Width;
            //rectangle.Height = ArtEvaluator.targetBitmap.Height;
            //rectangle.Stroke = Brushes.Black;
            //rectangle.StrokeThickness = 5.0;
            //canvas.Children.Add(rectangle);
            //mainWindow.Content = canvas;

            
            rtb.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate()
            {
                ArtEvaluator.ConvertFEtoRTB(canvas,ref rtb);
                genWindow.Background = new ImageBrush(rtb);
            }));

            return "GA Plotted";
        }


        internal string randBackground()
        {
            string result = "";
            byte r = (byte)G.random.Next(256);
            byte g = (byte)G.random.Next(256);
            byte b = (byte)G.random.Next(256);
            mainWindow.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
            result = "Back color changed to RGB(" + r.ToString() + "," + g.ToString() + "," + b.ToString() + ")";
            return result;
        }



    }
}
