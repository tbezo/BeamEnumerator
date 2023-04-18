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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace BeamEnumerator
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        private ScriptContext context;
        public DRRCalculationParameters drrParam = new DRRCalculationParameters(500);  // 50 cm DRR size
        private string beamIdx;
        public string BeamIdx
        {
            get { return beamIdx; }
            set {

                    beamIdx = value;
                    if(value.Length >= 3)
                    {
                        beamIdx = value.Substring(0,3);
                    }
                    else if(value.Length < 3)
                    {
                        beamIdx = value;
                        while(beamIdx.Length < 3) { BeamIdx += "1"; }
                    }                    
                }
        }

        public UserControl1(ScriptContext context)
        {
            InitializeComponent();
            DataContext = this;
            this.context = context;
            BeamIdx = "111"; //default value in case gueassing fails
        }

        private void ButtonOK(object sender, RoutedEventArgs e)
        {
            AddSetupFields(context);
            EnumerateBeams(context);
            Window.GetWindow(this).Close();
            return;
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            return; // will not return script?!
        }

        private void KnochenButton_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
            drrParam.SetLayerParameters(1, 10.0, 100.0, 1000.0);
        }

        private void MammaButton_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 1.0, -450.0, 150.0, -1000.0, 1000.0);
        }

        private void ThoraxButton_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 1.0, -500.0, 1000.0, -020.0, -100.0);
            drrParam.SetLayerParameters(1, 2.0, -300.0, 800.0, -200.0, 50.0);
        }

        private void ExtremitaetButton_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 0.6, -990.0, 0.0, 10.0, 50.0);
            drrParam.SetLayerParameters(1, 0.1, -450.0, 150.0, -40.0, 80.0);
            drrParam.SetLayerParameters(2, 1.0, 100.0, 1000.0);
        }

        private void Seeds3Button_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 10.0, 1000.0, 4000.0, -30.0, 30.0);
            drrParam.SetLayerParameters(1, 0.1, 100.0, 1000.0);
        }

        private void Seeds5Button_Checked(object sender, RoutedEventArgs e)
        {
            drrParam = new DRRCalculationParameters(500);
            drrParam.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
            drrParam.SetLayerParameters(1, 10.0, 100.0, 1000.0);
        }


        // add setup beams if none exist
        private void AddSetupFields(ScriptContext context)
        {
            bool setupDirection = false;
            
            if (context.ExternalPlanSetup.Beams.Where(x => x.IsSetupField).Count() == 0)
            {
                // Setup Beam Machine Parameters
                string linac = context.ExternalPlanSetup.Beams.First().TreatmentUnit.Id;
                VVector iso = context.ExternalPlanSetup.Beams.First().IsocenterPosition;
                ExternalBeamMachineParameters mParams = new ExternalBeamMachineParameters(linac, "6X", 300, "STATIC", null); // dose rate correct?

                // check if there are beams between 180 and 60 degree
                foreach (Beam b in context.ExternalPlanSetup.Beams)
                {
                    double ga = Math.Round(b.ControlPoints.First().GantryAngle, 0);
                    if (60.0 <= ga && ga <= 180 && !b.IsGantryExtended) { setupDirection = true; }
                    if (ga > 180.0 && b.IsGantryExtended) { setupDirection = true; } // also if there is a beam with extended angle > 180 
                }

                if (setupDirection) // if there is a field between 180 and 45 degree, add setup fields at 180 an 90 degree
                {
                    if (linac == "TrueBeam_2")
                    {
                        Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -125.0, 125.0, 125.0), 0.0, 180.0, 0.0, iso);
                    }
                    else
                    {
                        Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 180.0, 0.0, iso);
                    }
                    Beam nb2 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 90.0, 0.0, iso);
                }
                else // if there are no fields between 180 and 45 degree use gantry angles 0 and 270.
                {
                    if (linac == "TrueBeam_2")
                    {
                        Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -125.0, 125.0, 125.0), 0.0, 0.0, 0.0, iso);
                    }
                    else
                    {
                        Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 0.0, 0.0, iso);
                    }
                    Beam nb2 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 270.0, 0.0, iso);
                }
                string mbtext = "Setup Felder wurden erstellt, bitte die gewünschte\nToleranztabelle hinterlegen und die\nPlannormierung wieder einstellen!";
                MessageBox.Show(mbtext, "Info");
            }
        }

        // Change Beam Ids to nubmers according to plan name or user input
        private void EnumerateBeams(ScriptContext context)
        {
            int BeamCount = 1;
            string newBeamId = "";
            string gantryangle = "";
            string newBeamCount = "";
            // let the user order arc treatments by himself via the beam id
            if (context.PlanSetup.Beams.Where(x => !x.IsSetupField).First().Technique.ToString() == "ARC")
            {
                foreach (Beam b in context.PlanSetup.Beams.OrderBy(x => x.Id))
                {
                    b.Id = "x" + (20 - BeamCount).ToString(); // Fields are later named in descending order!
                    BeamCount++;
                }
            }
            else
            {
                // first beam loop is necessary to change all beam-IDs to unique ones (other than the script wants to produce). The new name is used for sorting CCW and grouping on table angles.
                foreach (Beam b in context.PlanSetup.Beams.OrderBy(y => y.ControlPoints.First().GantryAngle).ThenBy(x => JawArea(x.ControlPoints.First().JawPositions)).ThenBy(z => z.WeightFactor))
                {
                    double subnr = BeamCount;
                    if (b.ControlPoints.First().GantryAngle <= 180.0)
                    {
                        if (b.IsGantryExtended) { newBeamId = (b.ControlPoints.First().GantryAngle + subnr / 10).ToString(); }
                        else { newBeamId = (b.ControlPoints.First().GantryAngle + 360.0 + subnr / 10).ToString(); }
                    }
                    else
                    {
                        if (!b.IsGantryExtended) { newBeamId = (b.ControlPoints.First().GantryAngle + subnr / 10).ToString(); }
                        else { newBeamId = (b.ControlPoints.First().GantryAngle + 360.0 + subnr / 10).ToString(); }
                    }

                    if (b.ControlPoints.First().PatientSupportAngle != 0.0)
                    {
                        newBeamId = "_" + Math.Round(b.ControlPoints.First().PatientSupportAngle, 0).ToString("000") + newBeamId;
                    }
                    // left over from kiragroh BeamIdChanger script
                    b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId;
                    BeamCount++;
                }
            }

            //Treatment field-Loop
            BeamCount = 1;
            foreach (Beam b in context.PlanSetup.Beams.Where(x => !x.IsSetupField).OrderByDescending(y => y.Id)) //.OrderBy(y => y.Id) ControlPoints.First().GantryAngle
            {
                newBeamCount = BeamCount < 10 ? "0" + BeamCount.ToString() : BeamCount.ToString();
                newBeamId = BeamIdx + newBeamCount;
                // left over from kiragroh BeamIdChanger script
                b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId;
                BeamCount++;
                b.CreateOrReplaceDRR(drrParam);
            }

            //Setup field-Loop
            foreach (Beam b in context.PlanSetup.Beams.Where(x => x.IsSetupField)) //.OrderBy(y => y.Id)
            {
                StructureSet ss = context.PlanSetup.StructureSet;
                gantryangle = Math.Round(b.ControlPoints.First().GantryAngle, 0).ToString();
                newBeamCount = gantryangle.ToString().Length < 2 ? gantryangle.ToString() : gantryangle.ToString().Substring(gantryangle.ToString().Length - 2, 1);
                newBeamId = BeamIdx + "9" + newBeamCount;
                // left over from kiragroh BeamIdChanger script
                b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId;
                b.CreateOrReplaceDRR(drrParam);
            }
        }

        // calculate jaw area
        private double JawArea(VRect<double> jaws)
        {
            double area = (jaws.X2 - jaws.X1) * (jaws.Y2 - jaws.Y1);
            return area;
        }

    }
}
