using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Drawing;
using BeamEnumerator;


// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class Script
    {
        const string SCRIPT_NAME = "BeamEnumerator";

        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context , System.Windows.Window window/*, ScriptEnvironment environment*/)
        {
            if (context.ExternalPlanSetup == null)
            {
                MessageBox.Show("Please load a treatment plan");
                return;
            }

            var ui = new UserControl1(context);

            // decide on DRR parameters depending on plan name
            Regex mammaMatch = new Regex("MA");  // potential plan ID for breast plans
            Regex thoraxMatch = new Regex("TH");  // potential plan ID for thorax plans
            if (mammaMatch.IsMatch(context.PlanSetup.Id.ToUpper()))  // breast plan uses mamma DRR setting
            {
                //drrSelection = "Mamma DRR";
                ui.MammaButton.IsChecked = true;
                ui.drrParam.SetLayerParameters(0, 1.0, -450.0, 150.0, -1000.0, 1000.0);
            }
            else if (thoraxMatch.IsMatch(context.PlanSetup.Id.ToUpper())) // thorax plan uses thorax
            {
                //drrSelection = "Thorax DRR";
                ui.ThoraxButton.IsChecked = true;
                ui.drrParam.SetLayerParameters(0, 1.0, -500.0, 1000.0, -020.0, -100.0);
                ui.drrParam.SetLayerParameters(1, 2.0, -300.0, 800.0, -200.0, 50.0);
            }
            else  // all other plans use the default bone DRR setting
            {
                //drrSelection = "Knochen DRR";
                ui.KnochenButton.IsChecked = true;
                ui.drrParam.SetLayerParameters(0, 2.0, -16.0, 126.0, 20.0, 60.0);
                ui.drrParam.SetLayerParameters(1, 10.0, 100.0, 1000.0);
            }

                        
            // try to guess initial BeamIdx from reference point
            string refPtId = context.ExternalPlanSetup.ReferencePoints.OrderBy(x => x.Id).FirstOrDefault().Id;
            int index = Char.ToUpper(refPtId[1]) - 64; // converts character to number (A=1, B=2 etc.)
            if (Char.IsDigit(refPtId[0]) && Char.IsLetter(refPtId[1]))
            {
                ui.BeamIdx = refPtId[0] + index.ToString() + ui.BeamIdx.Remove(0, 2); //index could be more than one character!
            }
            else if (Char.IsDigit(refPtId[0]))
            {
                ui.BeamIdx = refPtId[0] + ui.BeamIdx.Remove(0, 1);
            }

          
            // Modifications start from here
            context.Patient.BeginModifications();

            window.Content = ui;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Title = "BeamEnumerator";
            //window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }
    }
}
