/*
** $Id: lstrlib.c,v 1.132.1.4 2008/07/11 17:27:21 roberto Exp $
** Standard library for string operations and pattern-matching
** See Copyright Notice in Linyee.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Linyee
{
	using ptrdiff_t = System.Int32;
	using ly_Integer = System.Int32;
	using LINYEE_INTFRM_T = System.Int64;
	using UNSIGNED_LINYEE_INTFRM_T = System.UInt64;

	public partial class Linyee
	{
		private static int str_len (LinyeeState L) {
		  uint l;
		  LinyeeLCheckLString(L, 1, out l);
		  LinyeePushInteger(L, (int)l);
		  return 1;
		}


		private static ptrdiff_t posrelat (ptrdiff_t pos, uint len) {
		  /* relative string position: negative means back from end */
		  if (pos < 0) pos += (ptrdiff_t)len + 1;
		  return (pos >= 0) ? pos : 0;
		}


		private static int str_sub (LinyeeState L) {
		  uint l;
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  ptrdiff_t start = posrelat(LinyeeLCheckInteger(L, 2), l);
		  ptrdiff_t end = posrelat(LinyeeLOptInteger(L, 3, -1), l);
		  if (start < 1) start = 1;
		  if (end > (ptrdiff_t)l) end = (ptrdiff_t)l;
		  if (start <= end)
			LinyeePushLString(L, s+start-1, (uint)(end-start+1));
		  else LinyeePushLiteral(L, "");
		  return 1;
		}


		private static int str_reverse (LinyeeState L) {
		  uint l;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  LinyeeLBuffInit(L, b);
		  while ((l--) != 0) LinyeeLAddChar(b, s[l]);
		  LinyeeLPushResult(b);
		  return 1;
		}


		private static int str_lower (LinyeeState L) {
		  uint l;
		  uint i;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  LinyeeLBuffInit(L, b);
		  for (i=0; i<l; i++)
			  LinyeeLAddChar(b, tolower(s[i]));
		  LinyeeLPushResult(b);
		  return 1;
		}


		private static int str_upper (LinyeeState L) {
		  uint l;
		  uint i;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  LinyeeLBuffInit(L, b);
		  for (i=0; i<l; i++)
			  LinyeeLAddChar(b, toupper(s[i]));
		  LinyeeLPushResult(b);
		  return 1;
		}

		private static int str_rep (LinyeeState L) {
		  uint l;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  int n = LinyeeLCheckInt(L, 2);
		  LinyeeLBuffInit(L, b);
		  while (n-- > 0)
			LinyeeLAddLString(b, s, l);
		  LinyeeLPushResult(b);
		  return 1;
		}


		private static int str_byte (LinyeeState L) {
		  uint l;
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  ptrdiff_t posi = posrelat(LinyeeLOptInteger(L, 2, 1), l);
		  ptrdiff_t pose = posrelat(LinyeeLOptInteger(L, 3, posi), l);
		  int n, i;
		  if (posi <= 0) posi = 1;
		  if ((uint)pose > l) pose = (int)l;
		  if (posi > pose) return 0;  /* empty interval; return no values */
		  n = (int)(pose -  posi + 1);
		  if (posi + n <= pose)  /* overflow? */
			LinyeeLError(L, "string slice too long");
		  LinyeeLCheckStack(L, n, "string slice too long");
		  for (i=0; i<n; i++)
			  LinyeePushInteger(L, (byte)(s[posi + i - 1]));
		  return n;
		}


		private static int str_char (LinyeeState L) {
		  int n = LinyeeGetTop(L);  /* number of arguments */
		  int i;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLBuffInit(L, b);
		  for (i=1; i<=n; i++) {
			int c = LinyeeLCheckInt(L, i);
			LinyeeLArgCheck(L, (byte)(c) == c, i, "invalid value");
			LinyeeLAddChar(b, (char)(byte)c);
		  }
		  LinyeeLPushResult(b);
		  return 1;
		}


		private static int writer (LinyeeState L, object b, uint size, object B)
		{
			if (b.GetType() != typeof(CharPtr))
			{
				using (MemoryStream stream = new MemoryStream())
				{
					// todo: figure out a way to do this
					/*
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, b);
					stream.Flush();
					byte[] bytes = stream.GetBuffer();
					char[] chars = new char[bytes.Length];
					for (int i = 0; i < bytes.Length; i++)
						chars[i] = (char)bytes[i];
					b = new CharPtr(chars);
					 * */
				}
			}
			LinyeeLAddLString((LinyeeLBuffer)B, (CharPtr)b, size);
		  return 0;
		}


		private static int str_dump (LinyeeState L) {
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLCheckType(L, 1, LINYEE_TFUNCTION);
		  LinyeeSetTop(L, 1);
		  LinyeeLBuffInit(L,b);
		  if (LinyeeDump(L, writer, b) != 0)
			LinyeeLError(L, "unable to dump given function");
		  LinyeeLPushResult(b);
		  return 1;
		}



		/*
		** {======================================================
		** PATTERN MATCHING
		** =======================================================
		*/


		public const int CAP_UNFINISHED	= (-1);
		public const int CAP_POSITION	= (-2);

		public class MatchState {

		  public MatchState()
		  {
			  for (int i = 0; i < LINYEE_MAXCAPTURES; i++)
				  capture[i] = new capture_();
		  }

		  public int matchdepth; /* control for recursive depth (to avoid C stack overflow) */
		  public CharPtr src_init;  /* init of source string */
		  public CharPtr src_end;  /* end (`\0') of source string */
		  public LinyeeState L;
		  public int level;  /* total number of captures (finished or unfinished) */

		  public class capture_{
			public CharPtr init;
			public ptrdiff_t len;
		  };
		  public capture_[] capture = new capture_[LINYEE_MAXCAPTURES];
		};


		public const int MAXCCALLS = 200;
		public const char L_ESC		= '%';
		public const string SPECIALS = "^$*+?.([%-";


		private static int check_capture (MatchState ms, int l) {
		  l -= '1';
		  if (l < 0 || l >= ms.level || ms.capture[l].len == CAP_UNFINISHED)
			return LinyeeLError(ms.L, "invalid capture index %%%d", l + 1);
		  return l;
		}


		private static int capture_to_close (MatchState ms) {
		  int level = ms.level;
		  for (level--; level>=0; level--)
			if (ms.capture[level].len == CAP_UNFINISHED) return level;
		  return LinyeeLError(ms.L, "invalid pattern capture");
		}


		private static CharPtr classend (MatchState ms, CharPtr p) {
		  p = new CharPtr(p);
		  char c = p[0];
		  p = p.next();
		  switch (c) {
			case L_ESC: {
			  if (p[0] == '\0')
				LinyeeLError(ms.L, "malformed pattern (ends with " + LINYEE_QL("%%") + ")");
			  return p+1;
			}
			case '[': {
			  if (p[0] == '^') p = p.next();
			  do {  /* look for a `]' */
				if (p[0] == '\0')
				  LinyeeLError(ms.L, "malformed pattern (missing " + LINYEE_QL("]") + ")");
				c = p[0];
				p = p.next();
				if (c == L_ESC && p[0] != '\0')
				  p = p.next();  /* skip escapes (e.g. `%]') */
			  } while (p[0] != ']');
			  return p+1;
			}
			default: {
			  return p;
			}
		  }
		}


		private static int match_class (int c, int cl) {
		  bool res;
		  switch (tolower(cl)) {
			case 'a' : res = isalpha(c); break;
			case 'c' : res = iscntrl(c); break;
			case 'd' : res = isdigit(c); break;
			case 'l' : res = islower(c); break;
			case 'p' : res = ispunct(c); break;
			case 's' : res = isspace(c); break;
			case 'u' : res = isupper(c); break;
			case 'w' : res = isalnum(c); break;
			case 'x' : res = isxdigit((char)c); break;
			case 'z' : res = (c == 0); break;
			default: return (cl == c) ? 1 : 0;
		  }
		  return (islower(cl) ? (res ? 1 : 0) : ((!res) ? 1 : 0));
		}


		private static int matchbracketclass (int c, CharPtr p, CharPtr ec) {
		  int sig = 1;
		  if (p[1] == '^') {
			sig = 0;
			p = p.next();  /* skip the `^' */
		  }
		  while ((p=p.next()) < ec) {
			if (p == L_ESC) {
			  p = p.next();
			  if (match_class(c, (byte)(p[0])) != 0)
				return sig;
			}
			else if ((p[1] == '-') && (p + 2 < ec)) {
			  p+=2;
			  if ((byte)((p[-2])) <= c && (c <= (byte)p[0]))
				return sig;
			}
			else if ((byte)(p[0]) == c) return sig;
		  }
		  return (sig == 0) ? 1 : 0;
		}


		private static int singlematch (int c, CharPtr p, CharPtr ep) {
		  switch (p[0]) {
			case '.': return 1;  /* matches any char */
			case L_ESC: return match_class(c, (byte)(p[1]));
			case '[': return matchbracketclass(c, p, ep-1);
		    default: return ((byte)(p[0]) == c) ? 1 : 0;
		  }
		}


		private static CharPtr matchbalance (MatchState ms, CharPtr s,
										   CharPtr p) {
		  if ((p[0] == 0) || (p[1] == 0))
			LinyeeLError(ms.L, "unbalanced pattern");
		  if (s[0] != p[0]) return null;
		  else {
			int b = p[0];
			int e = p[1];
			int cont = 1;
			while ((s=s.next()) < ms.src_end) {
			  if (s[0] == e) {
				if (--cont == 0) return s+1;
			  }
			  else if (s[0] == b) cont++;
			}
		  }
		  return null;  /* string ends out of balance */
		}


		private static CharPtr max_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  ptrdiff_t i = 0;  /* counts maximum expand for item */
		  while ( (s+i < ms.src_end) && (singlematch((byte)(s[i]), p, ep) != 0) )
			i++;
		  /* keeps trying to match with the maximum repetitions */
		  while (i>=0) {
			CharPtr res = match(ms, (s+i), ep+1);
			if (res != null) return res;
			i--;  /* else didn't match; reduce 1 repetition to try again */
		  }
		  return null;
		}


		private static CharPtr min_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  for (;;) {
			CharPtr res = match(ms, s, ep+1);
			if (res != null)
			  return res;
		  else if ( (s < ms.src_end) && (singlematch((byte)(s[0]), p, ep) != 0) )
			  s = s.next();  /* try with one more repetition */
			else return null;
		  }
		}


		private static CharPtr start_capture (MatchState ms, CharPtr s,
											CharPtr p, int what) {
		  CharPtr res;
		  int level = ms.level;
		  if (level >= LINYEE_MAXCAPTURES) LinyeeLError(ms.L, "too many captures");
		  ms.capture[level].init = s;
		  ms.capture[level].len = what;
		  ms.level = level+1;
		  if ((res=match(ms, s, p)) == null)  /* match failed? */
			ms.level--;  /* undo capture */
		  return res;
		}


		private static CharPtr end_capture(MatchState ms, CharPtr s,
										  CharPtr p) {
		  int l = capture_to_close(ms);
		  CharPtr res;
		  ms.capture[l].len = s - ms.capture[l].init;  /* close capture */
		  if ((res = match(ms, s, p)) == null)  /* match failed? */
			ms.capture[l].len = CAP_UNFINISHED;  /* undo capture */
		  return res;
		}


		private static CharPtr match_capture(MatchState ms, CharPtr s, int l)
		{
		  uint len;
		  l = check_capture(ms, l);
		  len = (uint)ms.capture[l].len;
		  if ((uint)(ms.src_end-s) >= len &&
			  memcmp(ms.capture[l].init, s, len) == 0)
			return s+len;
		  else return null;
		}


		private static CharPtr match(MatchState ms, CharPtr s, CharPtr p) {
		  s = new CharPtr(s);
		  p = new CharPtr(p);
		  bool runDflt = false;
		  bool runInit = true;
		  if (ms.matchdepth-- == 0)
			LinyeeLError(ms.L, "pattern too complex");
		  //init:
		  while (runInit) { // Replaces "init:" in order to be compatible with Mono.
			  runInit = false; // No "goto init" until further notice.
			  if (p != '\0') { /* end of pattern? */
				switch (p[0]) {
				  case '(': {  /* start capture */
					  if (p[1] == ')') {  /* position capture? */
						s = start_capture(ms, s, p + 2, CAP_POSITION);
					  }
					  else {
						s = start_capture(ms, s, p + 1, CAP_UNFINISHED);
					  }
					  break;
					}
				  case ')': {  /* end capture */
					  s = end_capture(ms, s, p + 1);
					  break;
					}
				  case '$': {
					  if (p[1] != '\0') {  /* is the `$' the last char in pattern? */
						runDflt = true; //goto dflt; /* no; go to default */
					  }
					  s = (s == ms.src_end) ? s : null;  /* check end of string */
					  break;
					}
				  case L_ESC: { /* escaped sequences not in the format class[*+?-]? */
					  switch (p[1]) {
						case 'b': {  /* balanced string? */
							s = matchbalance(ms, s, p + 2);
							if (s != null) {
							  p += 4;
							  runInit = true; //goto init;  /* return match(ms, s, p+4); */
							}
							/* else fail (s == NULL) */
							break;
						  }
						case 'f': {  /* frontier? */
							CharPtr ep; char previous;
							p += 2;
							if (p[0] != '[') {
							  LinyeeLError(ms.L, "missing " + LINYEE_QL("[") + " after " +
										  LINYEE_QL("%%f") + " in pattern");
							}
							ep = classend(ms, p);  /* points to what is next */
							previous = (s == ms.src_init) ? '\0' : s[-1];
							if ((matchbracketclass((byte)(previous), p, ep - 1) == 0) ||
							  (matchbracketclass((byte)(s[0]), p, ep - 1) != 0)) {
							  p = ep; 
							  runInit = true; //goto init; /* else return match(ms, s, ep); */
							}
							s = null;  /* match failed */
							break;
						  }
						default: {
							if (isdigit((byte)(p[1]))) {  /* capture results (%0-%9)? */
							  s = match_capture(ms, s, (byte)(p[1]));
							  if (s != null) {
								p += 2; 
								runInit = true; //goto init;  /* else return match(ms, s, p+2) */
							  }
							  break;
							}
							runDflt = true; //goto dflt;
							break;
						  }
					  }
					  break;
					}
				  default: {
					  runDflt = true; // goto dflt
					  break;
					}
				}
			  }

			  //dflt:
			  if (runDflt) // Replaces "dflt:" in order to be compatible with Mono.
			  {  /* pattern class plus optional suffix */
				  runDflt = false; // no more "goto dflt" until further notice.
				  CharPtr ep = classend(ms, p);  /* points to optional suffix */
				  /* does not match at least once? */
				  if ((s >= ms.src_end) || (singlematch((byte)(s[0]), p, ep) == 0)) {
					if (ep == '*' || ep == '?' || ep == '-') { /* accept empty? */
					  p = ep + 1; 
					  runInit = true; //goto init; /* return match(ms, s, ep + 1); */
					}
					else  /* '+' or no suffix */
					  s = null; /* fail */
				  }
				  else { /* matched once */
					switch (ep[0]) {
					  case '?': {  /* optional */
						  CharPtr res;
						  if ((res = match(ms, s + 1, ep + 1)) != null) {
							s = res;
						  }
						  else {
							p = ep + 1; 
							runInit = true; //goto init;  /* else return match(ms, s, ep+1); */
						  }
						  break;
						}
					  case '+': {  /* 1 or more repetitions */
						  s = s.next(); /* 1 match already done */
						  s = max_expand(ms, s, p, ep); // cannot fall through, repeating '*' instruction instead.
						  break;
						}
					  case '*': {  /* 0 or more repetitions */
						  s = max_expand(ms, s, p, ep);
						  break;
						}
					  case '-': {  /* 0 or more repetitions (minimum) */
						  s = min_expand(ms, s, p, ep);
						  break;
						}
					  default: { /* no suffix */
						  s = s.next(); 
						  p = ep; 
						  runInit = true; //goto init;  /* return match(ms, s+1, ep); */
						  break;
						}
					}
				  }
			  }
		  }
		  
		  ms.matchdepth++;
		  return s;
		}



		private static CharPtr lmemfind (CharPtr s1, uint l1,
									   CharPtr s2, uint l2) {
		  if (l2 == 0) return s1;  /* empty strings are everywhere */
		  else if (l2 > l1) return null;  /* avoids a negative `l1' */
		  else {
			CharPtr init;  /* to search for a `*s2' inside `s1' */
			l2--;  /* 1st char will be checked by `memchr' */
			l1 = l1-l2;  /* `s2' cannot be found after that */
			while (l1 > 0 && (init = memchr(s1, s2[0], l1)) != null) {
			  init = init.next();   /* 1st char is already checked */
			  if (memcmp(init, s2+1, l2) == 0)
				return init-1;
			  else {  /* correct `l1' and `s1' to try again */
				l1 -= (uint)(init-s1);
				s1 = init;
			  }
			}
			return null;  /* not found */
		  }
		}


		private static void push_onecapture (MatchState ms, int i, CharPtr s,
															CharPtr e) {
		  if (i >= ms.level) {
			if (i == 0)  /* ms.level == 0, too */
			  LinyeePushLString(ms.L, s, (uint)(e - s));  /* add whole match */
			else
			  LinyeeLError(ms.L, "invalid capture index");
		  }
		  else {
			ptrdiff_t l = ms.capture[i].len;
			if (l == CAP_UNFINISHED) LinyeeLError(ms.L, "unfinished capture");
			if (l == CAP_POSITION)
			  LinyeePushInteger(ms.L, ms.capture[i].init - ms.src_init + 1);
			else
			  LinyeePushLString(ms.L, ms.capture[i].init, (uint)l);
		  }
		}


		private static int push_captures (MatchState ms, CharPtr s, CharPtr e) {
		  int i;
		  int nlevels = ((ms.level == 0) && (s!=null)) ? 1 : ms.level;
		  LinyeeLCheckStack(ms.L, nlevels, "too many captures");
		  for (i = 0; i < nlevels; i++)
			push_onecapture(ms, i, s, e);
		  return nlevels;  /* number of strings pushed */
		}


		private static int str_find_aux (LinyeeState L, int find) {
		  uint l1, l2;
		  CharPtr s = LinyeeLCheckLString(L, 1, out l1);
		  CharPtr p = LinyeeLCheckLString(L, 2, out l2);
		  ptrdiff_t init = posrelat(LinyeeLOptInteger(L, 3, 1), l1) - 1;
		  if (init < 0) init = 0;
		  else if ((uint)(init) > l1) init = (ptrdiff_t)l1;
		  if ((find!=0) && ((LinyeeToBoolean(L, 4)!=0) ||  /* explicit request? */
			  strpbrk(p, SPECIALS) == null)) {  /* or no special characters? */
			/* do a plain search */
			CharPtr s2 = lmemfind(s+init, (uint)(l1-init), p, (uint)(l2));
			if (s2 != null) {
			  LinyeePushInteger(L, s2-s+1);
			  LinyeePushInteger(L, (int)(s2-s+l2));
			  return 2;
			}
		  }
		  else {
			MatchState ms = new MatchState();
			int anchor = 0;
			if (p[0] == '^')
			{
				p = p.next();
				anchor = 1;
			}
			CharPtr s1=s+init;
			ms.L = L;
			ms.matchdepth = MAXCCALLS;
			ms.src_init = s;
			ms.src_end = s+l1;
			do {
			  CharPtr res;
			  ms.level = 0;
			  LinyeeAssert(ms.matchdepth == MAXCCALLS);
			  if ((res=match(ms, s1, p)) != null) {
				if (find != 0) {
				  LinyeePushInteger(L, s1-s+1);  /* start */
				  LinyeePushInteger(L, res-s);   /* end */
				  return push_captures(ms, null, null) + 2;
				}
				else
				  return push_captures(ms, s1, res);
			  }
			} while (((s1=s1.next()) <= ms.src_end) && (anchor==0));
		  }
		  LinyeePushNil(L);  /* not found */
		  return 1;
		}


		private static int str_find (LinyeeState L) {
		  return str_find_aux(L, 1);
		}


		private static int str_match (LinyeeState L) {
		  return str_find_aux(L, 0);
		}


		private static int gmatch_aux (LinyeeState L) {
		  MatchState ms = new MatchState();
		  uint ls;
		  CharPtr s = LinyeeToLString(L, LinyeeUpValueIndex(1), out ls);
		  CharPtr p = LinyeeToString(L, LinyeeUpValueIndex(2));
		  CharPtr src;
		  ms.L = L;
		  ms.matchdepth = MAXCCALLS;
		  ms.src_init = s;
		  ms.src_end = s+ls;
		  for (src = s + (uint)LinyeeToInteger(L, LinyeeUpValueIndex(3));
			   src <= ms.src_end;
			   src = src.next()) {
			CharPtr e;
			ms.level = 0;
			LinyeeAssert(ms.matchdepth == MAXCCALLS);
			if ((e = match(ms, src, p)) != null) {
			  ly_Integer newstart = e-s;
			  if (e == src) newstart++;  /* empty match? go at least one position */
			  LinyeePushInteger(L, newstart);
			  LinyeeReplace(L, LinyeeUpValueIndex(3));
			  return push_captures(ms, src, e);
			}
		  }
		  return 0;  /* not found */
		}


		private static int gmatch (LinyeeState L) {
		  LinyeeLCheckString(L, 1);
		  LinyeeLCheckString(L, 2);
		  LinyeeSetTop(L, 2);
		  LinyeePushInteger(L, 0);
		  LinyeePushCClosure(L, gmatch_aux, 3);
		  return 1;
		}


		private static int gfind_nodef (LinyeeState L) {
		  return LinyeeLError(L, LINYEE_QL("string.gfind") + " was renamed to " +
							   LINYEE_QL("string.gmatch"));
		}


		private static void add_s (MatchState ms, LinyeeLBuffer b, CharPtr s,
														   CharPtr e) {
		  uint l, i;
		  CharPtr news = LinyeeToLString(ms.L, 3, out l);
		  for (i = 0; i < l; i++) {
			if (news[i] != L_ESC)
			  LinyeeLAddChar(b, news[i]);
			else {
			  i++;  /* skip ESC */
			  if (!isdigit((byte)(news[i])))
				LinyeeLAddChar(b, news[i]);
			  else if (news[i] == '0')
				  LinyeeLAddLString(b, s, (uint)(e - s));
			  else {
				push_onecapture(ms, news[i] - '1', s, e);
				LinyeeLAddValue(b);  /* add capture to accumulated result */
			  }
			}
		  }
		}


		private static void add_value (MatchState ms, LinyeeLBuffer b, CharPtr s,
															   CharPtr e) {
		  LinyeeState L = ms.L;
		  switch (LinyeeType(L, 3)) {
			case LINYEE_TNUMBER:
			case LINYEE_TSTRING: {
			  add_s(ms, b, s, e);
			  return;
			}
			case LINYEE_TUSERDATA:
			case LINYEE_TFUNCTION: {
			  int n;
			  LinyeePushValue(L, 3);
			  n = push_captures(ms, s, e);
			  LinyeeCall(L, n, 1);
			  break;
			}
			case LINYEE_TTABLE: {
			  push_onecapture(ms, 0, s, e);
			  LinyeeGetTable(L, 3);
			  break;
			}
		  }
		  if (LinyeeToBoolean(L, -1)==0) {  /* nil or false? */
			LinyeePop(L, 1);
			LinyeePushLString(L, s, (uint)(e - s));  /* keep original text */
		  }
		  else if (LinyeeIsString(L, -1)==0)
			LinyeeLError(L, "invalid replacement value (a %s)", LinyeeLTypeName(L, -1)); 
		  LinyeeLAddValue(b);  /* add result to accumulator */
		}


		private static int str_gsub (LinyeeState L) {
		  uint srcl;
		  CharPtr src = LinyeeLCheckLString(L, 1, out srcl);
		  CharPtr p = LinyeeLCheckString(L, 2);
		  int  tr = LinyeeType(L, 3);
		  int max_s = LinyeeLOptInt(L, 4, (int)(srcl+1));
		  int anchor = 0;
		  if (p[0] == '^')
		  {
			  p = p.next();
			  anchor = 1;
		  }
		  int n = 0;
		  MatchState ms = new MatchState();
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLArgCheck(L, tr == LINYEE_TNUMBER || tr == LINYEE_TSTRING ||
						   tr == LINYEE_TFUNCTION || tr == LINYEE_TTABLE ||
						   tr == LINYEE_TUSERDATA, 3,
							  "string/function/table expected");
		  LinyeeLBuffInit(L, b);
		  ms.L = L;
		  ms.matchdepth = MAXCCALLS;
		  ms.src_init = src;
		  ms.src_end = src+srcl;
		  while (n < max_s) {
			CharPtr e;
			ms.level = 0;
			LinyeeAssert(ms.matchdepth == MAXCCALLS);
			e = match(ms, src, p);
			if (e != null) {
			  n++;
			  add_value(ms, b, src, e);
			}
			if ((e!=null) && e>src) /* non empty match? */
			  src = e;  /* skip it */
			else if (src < ms.src_end)
			{
				char c = src[0];
				src = src.next();
				LinyeeLAddChar(b, c);
			}
			else break;
			if (anchor != 0) break;
		  }
		  LinyeeLAddLString(b, src, (uint)(ms.src_end-src));
		  LinyeeLPushResult(b);
		  LinyeePushInteger(L, n);  /* number of substitutions */
		  return 2;
		}

		/* }====================================================== */


		/* maximum size of each formatted item (> len(format('%99.99f', -1e308))) */
		public const int MAX_ITEM	= 512;
		/* valid flags in a format specification */
		public const string FLAGS = "-+ #0";
		/*
		** maximum size of each format specification (such as '%-099.99d')
		** (+10 accounts for %99.99x plus margin of error)
		*/
		public static readonly int MAX_FORMAT = (FLAGS.Length+1) + (LINYEE_INTFRMLEN.Length+1) + 10;


		private static void addquoted (LinyeeState L, LinyeeLBuffer b, int arg) {
		  uint l;
		  CharPtr s = LinyeeLCheckLString(L, arg, out l);
		  LinyeeLAddChar(b, '"');
		  while ((l--) != 0) {
			switch (s[0]) {
			  case '"': case '\\': case '\n': {
				LinyeeLAddChar(b, '\\');
				LinyeeLAddChar(b, s[0]);
				break;
			  }
			  case '\r': {
				LinyeeLAddLString(b, "\\r", 2);
				break;
			  }
			  case '\0': {
				LinyeeLAddLString(b, "\\000", 4);
				break;
			  }
			  default: {
				LinyeeLAddChar(b, s[0]);
				break;
			  }
			}
			s = s.next();
		  }
		  LinyeeLAddChar(b, '"');
		}

		private static CharPtr scanformat (LinyeeState L, CharPtr strfrmt, CharPtr form) {
		  CharPtr p = strfrmt;
		  while (p[0] != '\0' && strchr(FLAGS, p[0]) != null) p = p.next();  /* skip flags */
		  if ((uint)(p - strfrmt) >= (FLAGS.Length+1))
			LinyeeLError(L, "invalid format (repeated flags)");
		  if (isdigit((byte)(p[0]))) p = p.next();  /* skip width */
		  if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  if (p[0] == '.') {
			p = p.next();
			if (isdigit((byte)(p[0]))) p = p.next();  /* skip precision */
			if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  }
		  if (isdigit((byte)(p[0])))
			LinyeeLError(L, "invalid format (width or precision too long)");
		  form[0] = '%';
		  form = form.next();
		  strncpy(form, strfrmt, p - strfrmt + 1);
		  form += p - strfrmt + 1;
		  form[0] = '\0';
		  return p;
		}


		private static void addintlen (CharPtr form) {
		  uint l = (uint)strlen(form);
		  char spec = form[l - 1];
		  strcpy(form + l - 1, LINYEE_INTFRMLEN);
		  form[l + (LINYEE_INTFRMLEN.Length + 1) - 2] = spec;
		  form[l + (LINYEE_INTFRMLEN.Length + 1) - 1] = '\0';
		}


		private static int str_format (LinyeeState L) {
		  int top = LinyeeGetTop(L);
		  int arg = 1;
		  uint sfl;
		  CharPtr strfrmt = LinyeeLCheckLString(L, arg, out sfl);
		  CharPtr strfrmt_end = strfrmt+sfl;
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLBuffInit(L, b);
		  while (strfrmt < strfrmt_end) {
			  if (strfrmt[0] != L_ESC)
			  {
				  LinyeeLAddChar(b, strfrmt[0]);
				  strfrmt = strfrmt.next();
			  }
			  else if (strfrmt[1] == L_ESC)
			  {
				  LinyeeLAddChar(b, strfrmt[0]);  /* %% */
				  strfrmt = strfrmt + 2;
			  }
			  else
			  { /* format item */
				  strfrmt = strfrmt.next();
				  CharPtr form = new char[MAX_FORMAT];  /* to store the format (`%...') */
				  CharPtr buff = new char[MAX_ITEM];  /* to store the formatted item */
				  if (++arg > top)
				      LinyeeLArgError(L, arg, "no value");
				  strfrmt = scanformat(L, strfrmt, form);
				  char ch = strfrmt[0];
				  strfrmt = strfrmt.next();
				  switch (ch)
				  {
					  case 'c':
						  {
							  sprintf(buff, form, (int)LinyeeLCheckNumber(L, arg));
							  break;
						  }
					  case 'd':
					  case 'i':
						  {
							  addintlen(form);
							  sprintf(buff, form, (LINYEE_INTFRM_T)LinyeeLCheckNumber(L, arg));
							  break;
						  }
					  case 'o':
					  case 'u':
					  case 'x':
					  case 'X':
						  {
							  addintlen(form);
							  sprintf(buff, form, (UNSIGNED_LINYEE_INTFRM_T)LinyeeLCheckNumber(L, arg));
							  break;
						  }
					  case 'e':
					  case 'E':
					  case 'f':
					  case 'g':
					  case 'G':
						  {
							  sprintf(buff, form, (double)LinyeeLCheckNumber(L, arg));
							  break;
						  }
					  case 'q':
						  {
							  addquoted(L, b, arg);
							  continue;  /* skip the 'addsize' at the end */
						  }
					  case 's':
						  {
							  uint l;
							  CharPtr s = LinyeeLCheckLString(L, arg, out l);
							  if ((strchr(form, '.') == null) && l >= 100)
							  {
								  /* no precision and string is too long to be formatted;
									 keep original string */
								  LinyeePushValue(L, arg);
								  LinyeeLAddValue(b);
								  continue;  /* skip the `addsize' at the end */
							  }
							  else
							  {
								  sprintf(buff, form, s);
								  break;
							  }
						  }
					  default:
						  {  /* also treat cases `pnLlh' */
							  return LinyeeLError(L, "invalid option " + LINYEE_QL("%%%c") + " to " +
												   LINYEE_QL("format"), strfrmt[-1]);
						  }
				  }
				  LinyeeLAddLString(b, buff, (uint)strlen(buff));
			  }
		  }
		  LinyeeLPushResult(b);
		  return 1;
		}


		private readonly static LinyeeLReg[] strlib = {
		  new LinyeeLReg("byte", str_byte),
		  new LinyeeLReg("char", str_char),
		  new LinyeeLReg("dump", str_dump),
		  new LinyeeLReg("find", str_find),
		  new LinyeeLReg("format", str_format),
		  new LinyeeLReg("gfind", gfind_nodef),
		  new LinyeeLReg("gmatch", gmatch),
		  new LinyeeLReg("gsub", str_gsub),
		  new LinyeeLReg("len", str_len),
		  new LinyeeLReg("lower", str_lower),
		  new LinyeeLReg("match", str_match),
		  new LinyeeLReg("rep", str_rep),
		  new LinyeeLReg("reverse", str_reverse),
		  new LinyeeLReg("sub", str_sub),
		  new LinyeeLReg("upper", str_upper),
		  new LinyeeLReg(null, null)
		};


		private static void createmetatable (LinyeeState L) {
		  LinyeeCreateTable(L, 0, 1);  /* create metatable for strings */
		  LinyeePushLiteral(L, "");  /* dummy string */
		  LinyeePushValue(L, -2);
		  LinyeeSetMetatable(L, -2);  /* set string metatable */
		  LinyeePop(L, 1);  /* pop dummy string */
		  LinyeePushValue(L, -2);  /* string library... */
		  LinyeeSetField(L, -2, "__index");  /* ...is the __index metamethod */
		  LinyeePop(L, 1);  /* pop metatable */
		}


		/*
		** Open string library
		*/
		public static int luaopen_string (LinyeeState L) {
		  LinyeeLRegister(L, LINYEE_STRLIBNAME, strlib);
		#if LINYEE_COMPAT_GFIND
		  ly_getfield(L, -1, "gmatch");
		  ly_setfield(L, -2, "gfind");
		#endif
		  createmetatable(L);
		  return 1;
		}

	}
}
