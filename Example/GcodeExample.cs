﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOT.ClientExample
{
    public static class GcodeExample
    {
        public static string MIC_Pos  // Check coordinates for MICrophone speaker
        {
            get
            {
                return
@"(ToolToUse:Finger1)

N000 F150 E5000 E-5000
N010 G17

('MIC Pos')
N100 G01 Y142.923 X55.759 Z1
";
            }
        }

        public static string GCode
        {
            get
            {
                return
@"(ToolToUse:Finger1)

N000 F500 E10000 E-10000
N010 G17

('X=133.964, Y=41.959, Z=70')

('Power button ')
N020 G01 X133.964 Y41.984 Z1
N030 G01 X133.964 Y41.984 Z67
N040 G01 X133.964 Y41.984 Z73.5
(N060 G01 X133.964 Y41.984 Z67)

('Button 1')
N070 G01 X142.923 Y40.588 Z67
N080 G01 X142.923 Y40.588 Z73.5
(N090 G01 X142.923 Y40.588 Z67)

('Button 2')
N100 G01 X142.923 Y55.759 Z67
N110 G01 X142.923 Y55.759 Z73.5
(N120 G01 X142.923 Y55.759 Z67)

('Button 3')
N130 G01 X142.923 Y71.814 Z67
N140 G01 X142.923 Y71.814 Z73.5
(N150 G01 X142.923 Y71.814 Z67)

('Button 4')
N160 G01 X154.940 Y73.448 Z67
N170 G01 X154.940 Y73.448 Z73.5
(N180 G01 X154.940 Y73.448 Z67)

('Button 5')
N190 G01 X154.940 Y55.778 Z67
N200 G01 X154.940 Y55.778 Z73.2
(N210 G01 X154.940 Y55.778 Z67)

('Button 6')
N220 G01 X154.940 Y38.754 Z67
N230 G01 X154.940 Y38.754 Z73.5
(N240 G01 X154.940 Y38.754 Z67)

('Button 7')
N250 G01 X167.756 Y42.098 Z67
N260 G01 X167.756 Y42.098 Z73.2
(N270 G01 X167.756 Y42.098 Z67)

('Button 8')
N280 G01 X167.756 Y55.883 Z67
N290 G01 X167.756 Y55.883 Z73.3
(N300 G01 X167.756 Y55.883 Z67)

('Button 9')
N310 G01 X167.756 Y69.886 Z67
N320 G01 X167.756 Y69.886 Z73.2
(N330 G01 X167.756 Y69.886 Z67)";
            }
        }
    }
}