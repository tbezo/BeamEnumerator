﻿using System;
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
        private DRRCalculationParameters drrParamBone = new DRRCalculationParameters(500);
        private string mbtext = "";
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
            // extra parameter set for Mamma Dok0
            drrParamBone.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
            drrParamBone.SetLayerParameters(1, 10.0, 100.0, 1000.0);
        }

        private void ButtonOK(object sender, RoutedEventArgs e)
        {
            // add default setup beams if none exist
            if (context.ExternalPlanSetup.Beams.Where(x => x.IsSetupField).Count() == 0)
            { 
                if (!MammaButton.IsChecked.Value)
                {
                    AddSetupFields(context);
                }
                else
                {
                    AddMammaSetupFields(context);
                }
            }           
            EnumerateBeams(context);
            if (mbtext.Length > 0) { MessageBox.Show(mbtext, "Info"); }
            Window.GetWindow(this).Close();
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


        // add default setup beams
        private void AddSetupFields(ScriptContext context)
        {
            bool setupDirection = false;
            
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

            // if there is a field between 180 and 60 degree, add setup fields at 180 an 90 degree
            if (setupDirection) 
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
            // if there are no fields between 180 and 45 degree use gantry angles 0 and 270.
            else
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
            this.mbtext += "Setup Felder wurden erstellt, bitte die gewünschte\nToleranztabelle hinterlegen und die Plannormierung\nwieder einstellen!";
        }

        // add mamma setup beams
        private void AddMammaSetupFields(ScriptContext context)
        {
            bool setupDirection = false;

            // Setup Beam Machine Parameters
            string linac = context.ExternalPlanSetup.Beams.First().TreatmentUnit.Id;
            VVector iso = context.ExternalPlanSetup.Beams.First().IsocenterPosition;
            ExternalBeamMachineParameters mParams = new ExternalBeamMachineParameters(linac, "6X", 300, "STATIC", null);

            // test for left or right side breast, x>0 -> left x<0 -> right
            if (iso.x > 0) { setupDirection = true; }

            // if left sided tumor use 300°
            if (setupDirection)
            {
                if (linac == "TrueBeam_2")
                {
                    Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -125.0, 125.0, 125.0), 0.0, 300.0, 0.0, iso);
                }
                else
                {
                    Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 300.0, 0.0, iso);
                }
            }
            // else: right sided tumor use 60°
            else
            {
                if (linac == "TrueBeam_2")
                {
                    Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -125.0, 125.0, 125.0), 0.0, 60.0, 0.0, iso);
                }
                else
                {
                    Beam nb1 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 60.0, 0.0, iso);
                }
            }

            if (context.ExternalPlanSetup.ReferencePoints.Count() > 2) // if more than two reference points we assume the is a supra clav ptv which get's an extra setup field
            {
                if (linac == "TrueBeam_2")
                {
                    Beam nb2 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -125.0, 125.0, 125.0), 0.0, 00.0, 0.0, iso);
                }
                else
                {
                    Beam nb2 = context.ExternalPlanSetup.AddSetupBeam(mParams, new VRect<double>(-125.0, -90.0, 125.0, 90.0), 0.0, 00.0, 0.0, iso);
                }
            }
            this.mbtext += "Mamma Setup Felder wurden erstellt, bitte die gewünschte\nToleranztabelle hinterlegen und die Plannormierung\nwieder einstellen!";
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
                newBeamCount = BeamCount < 10 ? "0" + BeamCount.ToString() : BeamCount.ToString(); // handle single and double digits
                newBeamId = BeamIdx + newBeamCount;
                // left over from kiragroh BeamIdChanger script
                b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId; 
                BeamCount++;
                b.CreateOrReplaceDRR(drrParam);
            }

            // Setup field-Loop for normal setup fields
            if (!MammaButton.IsChecked.Value)
            {
                foreach (Beam b in context.PlanSetup.Beams.Where(x => x.IsSetupField)) //.OrderBy(y => y.Id)
                {
                    gantryangle = Math.Round(b.ControlPoints.First().GantryAngle, 0).ToString();
                    newBeamCount = gantryangle.ToString().Length < 2 ? gantryangle.ToString() : gantryangle.ToString().Substring(gantryangle.ToString().Length - 2, 1);
                    newBeamId = BeamIdx + "9" + newBeamCount;

                    try // changing beam id, info if it fails
                    {   
                        // left over from kiragroh BeamIdChanger script
                        b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId;
                    }
                    catch { mbtext += "\nNummerierung der Dokfelder nicht eindeutig, bitte händisch korrigieren"; }
                    b.CreateOrReplaceDRR(drrParam);
                }
            }
            else // DokMedLat and Dok0
            {
                foreach (Beam b in context.PlanSetup.Beams.Where(x => x.IsSetupField)) //.OrderBy(y => y.Id)
                {
                    List<string> defAngles = new List<string> { "90", "270", "180" }; // "normal" setup field angles 
                    gantryangle = Math.Round(b.ControlPoints.First().GantryAngle, 0).ToString();

                    // decide on Beam Ids by gantry angle name, special cases are Dok0 and DokMedLat
                    if (gantryangle == "0")
                    {
                        newBeamId = "Dok0";
                    }
                    else if (defAngles.Contains(gantryangle))
                    {
                        newBeamCount = gantryangle.ToString().Length < 2 ? gantryangle.ToString() : gantryangle.ToString().Substring(gantryangle.ToString().Length - 2, 1);
                        newBeamId = BeamIdx + "9" + newBeamCount;
                    }
                    else
                    {
                        newBeamId = "DokMedLat";
                    }

                    try // changing beam id, info if it fails
                    {   
                        // left over from kiragroh BeamIdChanger script
                        b.Id = newBeamId.Length > 16 ? newBeamId.Substring(0, 16) : newBeamId;
                    }
                    catch { mbtext += "\nBenennung der Dokfelder nicht eindeutig, bitte händisch korrigieren"; }

                    if (b.Id == "Dok0")
                    {
                        b.CreateOrReplaceDRR(drrParamBone); // Bone Rendering for supra clav setup field
                    }
                    else
                    {
                        b.CreateOrReplaceDRR(drrParam);
                    }
                }
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
