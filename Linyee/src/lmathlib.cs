/*
** $Id: lmathlib.c,v 1.67.1.1 2007/12/27 13:02:25 roberto Exp $
** Standard mathematical library
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	using LinyeeNumberType = System.Double;

	public partial class Linyee
	{
		public const double PI = 3.14159265358979323846;
		public const double RADIANS_PER_DEGREE = PI / 180.0;



		private static int MathAbs (LinyeeState L) {
		  LinyeePushNumber(L, Math.Abs(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathSin (LinyeeState L) {
		  LinyeePushNumber(L, Math.Sin(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathSinH (LinyeeState L) {
		  LinyeePushNumber(L, Math.Sinh(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathCos (LinyeeState L) {
		  LinyeePushNumber(L, Math.Cos(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathCosH (LinyeeState L) {
		  LinyeePushNumber(L, Math.Cosh(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathTan (LinyeeState L) {
		  LinyeePushNumber(L, Math.Tan(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathTanH (LinyeeState L) {
		  LinyeePushNumber(L, Math.Tanh(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathASin (LinyeeState L) {
		  LinyeePushNumber(L, Math.Asin(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathACos (LinyeeState L) {
		  LinyeePushNumber(L, Math.Acos(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathATan (LinyeeState L) {
		  LinyeePushNumber(L, Math.Atan(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathATan2 (LinyeeState L) {
		  LinyeePushNumber(L, Math.Atan2(LinyeeLCheckNumber(L, 1), LinyeeLCheckNumber(L, 2)));
		  return 1;
		}

		private static int MathCeil (LinyeeState L) {
		  LinyeePushNumber(L, Math.Ceiling(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathFloor (LinyeeState L) {
		  LinyeePushNumber(L, Math.Floor(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathFMod (LinyeeState L) {
		  LinyeePushNumber(L, fmod(LinyeeLCheckNumber(L, 1), LinyeeLCheckNumber(L, 2)));
		  return 1;
		}

		private static int MathModF (LinyeeState L) {
		  double ip;
		  double fp = modf(LinyeeLCheckNumber(L, 1), out ip);
		  LinyeePushNumber(L, ip);
		  LinyeePushNumber(L, fp);
		  return 2;
		}

		private static int MathSqrt (LinyeeState L) {
		  LinyeePushNumber(L, Math.Sqrt(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathPow (LinyeeState L) {
		  LinyeePushNumber(L, Math.Pow(LinyeeLCheckNumber(L, 1), LinyeeLCheckNumber(L, 2)));
		  return 1;
		}

		private static int MathLog (LinyeeState L) {
		  LinyeePushNumber(L, Math.Log(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathLog10 (LinyeeState L) {
		  LinyeePushNumber(L, Math.Log10(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathExp (LinyeeState L) {
		  LinyeePushNumber(L, Math.Exp(LinyeeLCheckNumber(L, 1)));
		  return 1;
		}

		private static int MathDeg (LinyeeState L) {
		  LinyeePushNumber(L, LinyeeLCheckNumber(L, 1)/RADIANS_PER_DEGREE);
		  return 1;
		}

		private static int MathRad (LinyeeState L) {
		  LinyeePushNumber(L, LinyeeLCheckNumber(L, 1)*RADIANS_PER_DEGREE);
		  return 1;
		}

		private static int MathFRExp (LinyeeState L) {
		  int e;
		  LinyeePushNumber(L, frexp(LinyeeLCheckNumber(L, 1), out e));
		  LinyeePushInteger(L, e);
		  return 2;
		}

		private static int MathLDExp (LinyeeState L) {
		  LinyeePushNumber(L, ldexp(LinyeeLCheckNumber(L, 1), LinyeeLCheckInt(L, 2)));
		  return 1;
		}



		private static int MahMin (LinyeeState L) {
		  int n = LinyeeGetTop(L);  /* number of arguments */
		  LinyeeNumberType dmin = LinyeeLCheckNumber(L, 1);
		  int i;
		  for (i=2; i<=n; i++) {
			LinyeeNumberType d = LinyeeLCheckNumber(L, i);
			if (d < dmin)
			  dmin = d;
		  }
		  LinyeePushNumber(L, dmin);
		  return 1;
		}


		private static int MathMax (LinyeeState L) {
		  int n = LinyeeGetTop(L);  /* number of arguments */
		  LinyeeNumberType dmax = LinyeeLCheckNumber(L, 1);
		  int i;
		  for (i=2; i<=n; i++) {
			LinyeeNumberType d = LinyeeLCheckNumber(L, i);
			if (d > dmax)
			  dmax = d;
		  }
		  LinyeePushNumber(L, dmax);
		  return 1;
		}

		private static Random rng = new Random();

		private static int MathRandom (LinyeeState L) {
		  /* the `%' avoids the (rare) case of r==1, and is needed also because on
			 some systems (SunOS!) `rand()' may return a value larger than RAND_MAX */
		  //LinyeeNumberType r = (LinyeeNumberType)(rng.Next()%RAND_MAX) / (LinyeeNumberType)RAND_MAX;
			LinyeeNumberType r = (LinyeeNumberType)rng.NextDouble();
		  switch (LinyeeGetTop(L)) {  /* check number of arguments */
			case 0: {  /* no arguments */
			  LinyeePushNumber(L, r);  /* Number between 0 and 1 */
			  break;
			}
			case 1: {  /* only upper limit */
			  int u = LinyeeLCheckInt(L, 1);
			  LinyeeLArgCheck(L, 1<=u, 1, "interval is empty");
			  LinyeePushNumber(L, Math.Floor(r*u)+1);  /* int between 1 and `u' */
			  break;
			}
			case 2: {  /* lower and upper limits */
			  int l = LinyeeLCheckInt(L, 1);
			  int u = LinyeeLCheckInt(L, 2);
			  LinyeeLArgCheck(L, l<=u, 2, "interval is empty");
			  LinyeePushNumber(L, Math.Floor(r * (u - l + 1)) + l);  /* int between `l' and `u' */
			  break;
			}
			default: return LinyeeLError(L, "wrong number of arguments");
		  }
		  return 1;
		}


		private static int MathRandomSeed (LinyeeState L) {
            // math.randomseed() can take a double number but Random expects an integer seed.
            // we use modulus to bring back the double to the allowed integer interval.
            LinyeeNumberType seed = Math.Abs(LinyeeLCheckNumber(L, 1));
            LinyeeNumberType max = (LinyeeNumberType)int.MaxValue;
            while (seed > max)
            {
                seed = fmod(seed, max);
            }

			rng = new Random((int)seed);
		  return 0;
		}


		private readonly static LinyeeLReg[] mathlib = {
		  new LinyeeLReg("abs",   MathAbs),
		  new LinyeeLReg("acos",  MathACos),
		  new LinyeeLReg("asin",  MathASin),
		  new LinyeeLReg("atan2", MathATan2),
		  new LinyeeLReg("atan",  MathATan),
		  new LinyeeLReg("ceil",  MathCeil),
		  new LinyeeLReg("cosh",   MathCosH),
		  new LinyeeLReg("cos",   MathCos),
		  new LinyeeLReg("deg",   MathDeg),
		  new LinyeeLReg("exp",   MathExp),
		  new LinyeeLReg("floor", MathFloor),
		  new LinyeeLReg("fmod",   MathFMod),
		  new LinyeeLReg("frexp", MathFRExp),
		  new LinyeeLReg("ldexp", MathLDExp),
		  new LinyeeLReg("log10", MathLog10),
		  new LinyeeLReg("log",   MathLog),
		  new LinyeeLReg("max",   MathMax),
		  new LinyeeLReg("min",   MahMin),
		  new LinyeeLReg("modf",   MathModF),
		  new LinyeeLReg("pow",   MathPow),
		  new LinyeeLReg("rad",   MathRad),
		  new LinyeeLReg("random",     MathRandom),
		  new LinyeeLReg("randomseed", MathRandomSeed),
		  new LinyeeLReg("sinh",   MathSinH),
		  new LinyeeLReg("sin",   MathSin),
		  new LinyeeLReg("sqrt",  MathSqrt),
		  new LinyeeLReg("tanh",   MathTanH),
		  new LinyeeLReg("tan",   MathTan),
		  new LinyeeLReg(null, null)
		};


		/*
		** Open math library
		*/
		public static int LinyeeOpenMath (LinyeeState L) {
		  LinyeeLRegister(L, LINYEE_MATHLIBNAME, mathlib);
		  LinyeePushNumber(L, PI);
		  LinyeeSetField(L, -2, "pi");
		  LinyeePushNumber(L, HUGE_VAL);
		  LinyeeSetField(L, -2, "huge");
		#if LINYEE_COMPAT_MOD
		  LinyeeGetField(L, -1, "fmod");
		  LinyeeSetField(L, -2, "mod");
		#endif
		  return 1;
		}

	}
}
