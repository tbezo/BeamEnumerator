using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Forms;
using System.Drawing;


// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class Script
    {

        const string SCRIPT_NAME = "BeamEnumerator";
        int BeamCount = 1;
        string newBeamId = "";
        string gantryangle = "";
        string newBeamCount = "";
        bool setupDirection = false;
        string BeamIdx = "111"; // default value
        private RadioButton selectedrb;
        string drrSelection = "Knochen DRR";
        DRRCalculationParameters drrParam = new DRRCalculationParameters(500);  // 50 cm DRR size

        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            if (context.ExternalPlanSetup == null)
            {
                MessageBox.Show("Please load a treatment plan");
                return;
            }

            // decide on DRR parameters depending on plan name
            Regex mammaMatch = new Regex("MA");  // potential plan ID for breast plans
            Regex thoraxMatch = new Regex("TH");  // potential plan ID for thorax plans
            if (mammaMatch.IsMatch(context.PlanSetup.Id.ToUpper()))  // breast plan uses mamma DRR setting
            {
                drrSelection = "Mamma DRR";
                drrParam.SetLayerParameters(0, 1.0, -450.0, 150.0, -1000.0, 1000.0);
            }
            else if (thoraxMatch.IsMatch(context.PlanSetup.Id.ToUpper())) // thorax plan uses thorax
            {
                drrSelection = "Thorax DRR";
                drrParam.SetLayerParameters(0, 1.0, -500.0, 1000.0, -020.0, -100.0);
                drrParam.SetLayerParameters(1, 2.0, -300.0, 800.0, -200.0, 50.0);
            }
            else  // all other plans use the default bone DRR setting
            {
                drrSelection = "Knochen DRR";
                drrParam.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
                drrParam.SetLayerParameters(1, 10.0, 100.0, 1000.0);
            }

                        
            // try to guess initial BeamIdx from reference point
            string refPtId = context.ExternalPlanSetup.ReferencePoints.OrderBy(x => x.Id).FirstOrDefault().Id;
            int index = Char.ToUpper(refPtId[1]) - 64; // converts character to number (A=1, B=2 etc.)
            if (Char.IsDigit(refPtId[0]) && Char.IsLetter(refPtId[1]))
            {
                BeamIdx = refPtId[0] + index.ToString() + BeamIdx.Remove(0, 2); //index could be more than one character!
            }
            else if (Char.IsDigit(refPtId[0]))
            {
                BeamIdx = refPtId[0] + BeamIdx.Remove(0, 1);
            }


            // ask for the first three digits for Field Number
            string value = BeamIdx;
            if (InputBox("Feldnummerierung", "Neue Feldnummer:", ref value) == DialogResult.OK)
            {
                if (value.Length > 2)
                {
                    BeamIdx = value.Substring(0, 3);
                }
                else
                {
                    MessageBox.Show("Bitte drei Ziffern eingeben");
                    return;
                }
            }
            else { return; }


            // Modifications start from here
            context.Patient.BeginModifications();

            // add setup beams if none exist
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
                MessageBox.Show(mbtext, "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

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
                    double subnr = BeamCount ;
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
                        newBeamId = "_" + Math.Round(b.ControlPoints.First().PatientSupportAngle,0).ToString("000") + newBeamId;
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

        static double JawArea(VRect<double> jaws)
        {
            double area = (jaws.X2 - jaws.X1) * (jaws.Y2 - jaws.Y1);
            return area;
        }

        // Draw Input Window.
        public DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            Label label2 = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            GroupBox gBox = new GroupBox();
            RadioButton rbutton1 = new RadioButton();
            RadioButton rbutton2 = new RadioButton();
            RadioButton rbutton3 = new RadioButton();
            RadioButton rbutton4 = new RadioButton();
            RadioButton rbutton5 = new RadioButton();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            rbutton1.Text = "Knochen DRR";
            rbutton2.Text = "Mamma DRR";
            rbutton3.Text = "Thorax DRR";
            rbutton4.Text = "Seeds 3cm DRR";
            rbutton5.Text = "Extremitäten DRR";

            if ( drrSelection == "Thorax DRR" )
            {
                rbutton3.Checked = true;
            }
            else if (drrSelection == "Mamma DRR")
            {
                rbutton2.Checked = true;
            }
            else
            {
                rbutton1.Checked = true;
            }

            rbutton1.CheckedChanged += new EventHandler(RadioButton_CheckedChanged);
            rbutton2.CheckedChanged += new EventHandler(RadioButton_CheckedChanged);
            rbutton3.CheckedChanged += new EventHandler(RadioButton_CheckedChanged);
            rbutton4.CheckedChanged += new EventHandler(RadioButton_CheckedChanged);
            rbutton5.CheckedChanged += new EventHandler(RadioButton_CheckedChanged);

            label2.Text = "Parametersatz";
            gBox.Controls.Add(rbutton1);
            gBox.Controls.Add(rbutton2);
            gBox.Controls.Add(rbutton3);
            gBox.Controls.Add(rbutton4);
            gBox.Controls.Add(rbutton5);

            label.SetBounds(9, 16, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            label2.SetBounds(9, 70, 372, 13);
            gBox.SetBounds(12, 83, 275, 122);
            rbutton1.SetBounds(10, 15, 130, 15);
            rbutton2.SetBounds(10, 35, 130, 15);
            rbutton3.SetBounds(10, 55, 130, 15);
            rbutton4.SetBounds(10, 75, 130, 15);
            rbutton5.SetBounds(10, 95, 130, 15);
            buttonOk.SetBounds(228, 218, 75, 23);
            buttonCancel.SetBounds(309, 218, 75, 23);

            label.AutoSize = true;
            label2.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            gBox.Anchor = AnchorStyles.Left;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 250);
            form.Controls.AddRange(new Control[] { label, textBox, label2, gBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        // Event handler for Changed Radio Button
        void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb == null)
            {
                MessageBox.Show("Sender is not a RadioButton");
                return;
            }

            // Ensure that the RadioButton.Checked property
            // changed to true.
            if (rb.Checked)
            {
                // Keep track of the selected RadioButton by saving a reference
                // to it. Change drrParams according to checked Radio Button Text.
                if (rb.Text == "Mamma DRR")  // breast plan uses mamma DRR setting
                {
                    drrParam = new DRRCalculationParameters(500);
                    drrParam.SetLayerParameters(0, 1.0, -450.0, 150.0, -1000.0, 1000.0);
                    //
                }
                else if (rb.Text == "Thorax DRR") // thorax plan uses thorax
                {
                    drrParam = new DRRCalculationParameters(500);
                    drrParam.SetLayerParameters(0, 1.0, -500.0, 1000.0, -020.0, -100.0);
                    drrParam.SetLayerParameters(1, 2.0, -300.0, 800.0, -200.0, 50.0);
                }
                else if (rb.Text == "Seeds 3cm DRR") // extremity plan
                {
                    drrParam = new DRRCalculationParameters(500);
                    drrParam.SetLayerParameters(0, 10.0, 1000.0, 4000.0, -30.0, 30.0);
                    drrParam.SetLayerParameters(1, 0.1, 100.0, 1000.0);
                }
                else if (rb.Text == "Extremitäten DRR") // extremity plan
                {
                    drrParam = new DRRCalculationParameters(500);
                    drrParam.SetLayerParameters(0, 0.6, -990.0, 0.0, 10.0, 50.0);
                    drrParam.SetLayerParameters(1, 0.1, -450.0, 150.0, -40.0, 80.0);
                    drrParam.SetLayerParameters(2, 1.0, 100.0, 1000.0);
                }
                else  // all other plan uses the default bone DRR setting
                {
                    drrParam = new DRRCalculationParameters(500);
                    drrParam.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
                    drrParam.SetLayerParameters(1, 10.0, 100.0, 1000.0);
                }

                drrSelection = rb.Text;
                selectedrb = rb;
            }
        }

    }
}
