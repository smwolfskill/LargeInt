using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms; //for MessageBox debug
namespace LargeNumberCalculator
{
    class LargeInt
    {
        /* Author: Scott Wolfskill
         * Created:       12/05/15
         * Last edited:   12/11/15 */ 
        /* Can perform normal computation on extremely large integers by representing them in binary using boolean arrays.
         *    Ex: 11 in base-10 = 1011 in binary = {T, T, F, T} (least-significant bit is the 0th index in the array)  */
        protected bool[] binaryDigits; //our LargeInt represented as a binary array where the 0th index is the LEAST significant digit.

        protected bool positive; //true indicates >= 0 (positive), false indicates < 0 (negative)
        protected int startIndex; //index of the most significant bit (useful b/c in almost all cases the entire array will not be full)

        public LargeInt() //default no-arg CTOR. Creates a 512-bit (Normal 32-bit * 10) signed integer.
        {
            binaryDigits = new bool[512];
            positive = true;
            startIndex = 0;
            for (int i = 0; i < binaryDigits.Length; i++)
                binaryDigits[i] = false;
        }

        public LargeInt(int bits) //better CTOR which allows user to create any signed integer with as many bits as they want.
        {
            if (bits < 1) throw new System.ArgumentException("Parameter must be 1 bit or more", "bits");
            binaryDigits = new bool[bits];
            positive = true;
            startIndex = 0;
            for (int i = 0; i < binaryDigits.Length; i++)
                binaryDigits[i] = false;
        }

        public LargeInt(LargeInt source) //copy CTOR
        {
            binaryDigits = new bool[source.bits()];
            positive = source.positive;
            startIndex = source.startIndex;
            for (int i = 0; i < source.bits(); i++)
                binaryDigits[i] = source.binaryDigits[i];
        }

        //Note for size, bits, usedBits, unusedBits: DOES NOT INCLUDE BIT FOR POS/NEG. That bit will always be there and is not relevant.
        public int size() //return size in bits of this LargeInt
        {
            return bits();
        }

        public int bits() //equivalent function to above: return size in bits of this LargeInt
        {
            return binaryDigits.Length;
        }

        public int usedBits() //return number of bits we have used in this LargeInt
        {
            return startIndex + 1;
        }

        public int unusedBits() //return number of bits we haven't used yet in this LargeInt
        {
            return binaryDigits.Length - startIndex - 1;
        }

        #region OPERATOR OVERLOADING: Logic: ==, !=, >, <, >=, <=   ; Operations: +, -, *, / ;  ++, --

        #region Logical Operations
        public bool Equals(LargeInt other)
        {
            return this == other;
        }

        public override bool Equals(object o)
        {
            try
            {
                return (bool)(this == (LargeInt) o);
            }
            catch
            {
                return false;
            }
        }

        public static bool operator ==(LargeInt a, LargeInt b)
        {
            if (a.positive != b.positive) return false; //preliminary check for signs. 
                        //Problem: What if they are both 0, but have different signs? Should always have 0 be positive.
            if (a.startIndex != b.startIndex) return false; //preliminary check for #digits
            for (int i = a.startIndex; i >= 0; i--) //Number of digits and sign matched up, so check each digit individually
            {
                if (a.binaryDigits[i] != b.binaryDigits[i]) return false;
            }
            return true;
        }

        public static bool operator !=(LargeInt a, LargeInt b)
        {
            return !(a == b); //simple. Call overloaded operator==
        }

        public static bool operator >(LargeInt a, LargeInt b)
        {
            return b < a; //only need to overload one of <, >
        }

        public static bool operator <(LargeInt a, LargeInt b)
        {
            if (a.positive && !b.positive) return false; //preliminary check for signs
                //Problem: What if they are both 0, but have different signs? Should always have 0 be positive.
            else if (!a.positive && b.positive) return true;

            if (a.startIndex > b.startIndex) return false; //preliminary check for #digits
            else if (a.startIndex < b.startIndex) return true; //if b has more digits, then a is obviously smaller
            //have same sign and same number of digits, so must go through them
            bool haveADifferingDigit = false;
            for (int i = a.startIndex; i >= 0; i--)
            {
                if(a.binaryDigits[i] && !b.binaryDigits[i]) return false;
                if (a.binaryDigits[i] != b.binaryDigits[i]) haveADifferingDigit = true;
            }
            //a < b if no digit of a is greater than the same digit in b and they contain at least 1 differing digit (so that means they are not equal)
            return haveADifferingDigit; 
        }

        public static bool operator >=(LargeInt a, LargeInt b)
        {
            //a > b || a == b: Call > first because it is faster to compute than ==
            if (a > b) return true;
            else return a == b; 
        }

        public static bool operator <=(LargeInt a, LargeInt b)
        {
            //a < b || a == b: Call < first because it is faster to compute than ==
            if (a < b) return true;
            else return a == b; 
        }
#endregion

        //Now the arithmetic overloading: +, -, *, / ;  ++, --
        public static LargeInt operator +(LargeInt a, LargeInt b)
        {
            //1. Calculate how many bits to use in the new LargeInt
            int newBits = a.bits() < b.bits() ? a.bits() : b.bits(); //have the default size as whichever one has less bits
            int combinedBits = a.usedBits() + b.usedBits();
            if (combinedBits > a.bits() && combinedBits > b.bits())
                newBits = 2 * combinedBits;
            else
            {
                if (b.bits() < a.bits() && combinedBits < a.bits() && combinedBits > b.bits()) // |b| < combinedBits < |a|
                    newBits = a.bits();
                else if (a.bits() < b.bits() && combinedBits < b.bits() && combinedBits > a.bits()) // |a| < combinedBits < |b|
                    newBits = b.bits();
                //else combinedBits < |a|, |b| so we just use the smaller of |a| and |b|
            }
            LargeInt sum = new LargeInt(newBits);
            //1. Determine signs
            //If both pos: Result is pos.
                //If a pos and a > |b|, result is pos. Ex:   4  + (-3) is pos because 4 > |-3|
                //If a neg and b > |a|, result is pos. Ex: (-3) +   4  is pos because 4 > |-3|
            /*if (a.positive && b.positive || a.positive && a > abs(b) || !a.positive && b > abs(a)) toReturn.positive = true;
            else toReturn.positive = false;*/
            //2. Check signs to determine if we should use this operator+ or call operator- .
            if (a.positive && !b.positive) //temporarily change b from neg to pos so we can do (+a) - (+b) instead of (+a) + (-b)
            {
                b.positive = true;
                sum = a - b; //call operator-
                b.positive = false;
                return sum;
            }
            else if (!a.positive && b.positive) //temporarily change a from neg to pos so we can do (+b) - (+a) instead of (-a) + (+b)
            {
                a.positive = true;
                sum = b - a; //call operator-
                a.positive = false;
                return sum;
            }
            else //3. Do the arithmetic
            {
                sum.positive = (a.positive && b.positive); //now they have to be the same sign. So if both pos, result is pos. If both neg, result is neg.
                int i = 0; //current index/bit. Start at least significant. Will become 0 at beginning of 1st iteration
                bool carryOver = false; //if adding 1 + 1, set bit to 0 and carryOver to true.
                bool nextCarryOver = false;
                while (i <= a.startIndex && i <= b.startIndex)
                {
                    //Now compare the two digits
                    if (a.binaryDigits[i] != b.binaryDigits[i]) sum.binaryDigits[i] = true; //0 + 1 = 1 + 0 = 1
                    else if (a.binaryDigits[i] && b.binaryDigits[i]) nextCarryOver = true; //1 + 1 = 0, but carry over the 1
                    //Don't need to consider 0 + 0 since toReturn[i] is initialized to 0 for all i
                    if (carryOver)
                    {
                        if (!sum.binaryDigits[i]) sum.binaryDigits[i] = true; //was just 0, so simply add the carryOver 1 and be done
                        else
                        { //1 is already there, so it is now 0 and we must carry over again
                            sum.binaryDigits[i] = false;
                            nextCarryOver = true;
                        }
                    }
                    i++;
                    carryOver = nextCarryOver;
                    nextCarryOver = false;
                }
                //If they were of different size, simply append the remaining digits from whichever one is longer
                if (a.startIndex > b.startIndex)
                {
                    sum.startIndex = a.startIndex;
                    while (i <= a.startIndex)
                    {
                        sum.binaryDigits[i] = a.binaryDigits[i];
                        if (carryOver)
                        {
                            if (!sum.binaryDigits[i])
                            {
                                sum.binaryDigits[i] = true; //was just 0, so simply add the carryOver 1 and be done
                                carryOver = false;
                            }
                            else
                            { //1 is already there, so it is now 0 and we must carry over again
                                sum.binaryDigits[i] = false;
                                carryOver = true;
                            }
                        }
                        i++;
                    }
                }
                else if (b.startIndex > a.startIndex)
                {
                    sum.startIndex = b.startIndex;
                    while (i <= b.startIndex)
                    {
                        sum.binaryDigits[i] = b.binaryDigits[i];
                        if (carryOver)
                        {
                            if (!sum.binaryDigits[i])
                            {
                                sum.binaryDigits[i] = true; //was just 0, so simply add the carryOver 1 and be done
                                carryOver = false;
                            }
                            else
                            { //1 is already there, so it is now 0 and we must carry over again
                                sum.binaryDigits[i] = false;
                                carryOver = true;
                            }
                        }
                        i++;
                    }
                }
                else sum.startIndex = a.startIndex; //case a.startIndex == b.startIndex
                if (carryOver) //if after all the adding we still need to carry over a 1, put it in the most significant digit
                {
                    //MessageBox.Show("LargInt(): operator+: Increasing size by one due to carryOver at end");
                    sum.binaryDigits[sum.startIndex + 1] = true;
                    sum.startIndex++;
                }

                //TESTING:
                printOperation(a, b, "+", sum);
                //END TESTING
                return sum;
            }
        }

        public static LargeInt operator -(LargeInt a, LargeInt b)
        {
            //1. Calculate how many bits to use in the new LargeInt
            int newBits = a.bits() < b.bits() ? a.bits() : b.bits(); //have the default size as whichever one has less bits
            LargeInt diff = new LargeInt(newBits);
            //2. Check signs to see if we should use this operator- or call operator+ . We want both a,b to be positive.
            if (a.positive && !b.positive) //temporarily change b to pos so we can do (+a) + (+b) instead of (+a) - (-b)
            {
                b.positive = true;
                diff = a + b; //call operator+
                b.positive = false;
                return diff;
            }
            else if (!a.positive && b.positive) //temporarily change b to neg so we can do (-a) + (-b) instead of (-a) - (+b)
            {
                b.positive = false;
                diff = a + b; //call operator+
                b.positive = true;
                return diff;
            }
            else if (!a.positive && !b.positive) //temporarily change both to pos so we can do (+b) - (+a) instead of (-a) - (-b)
            {
                a.positive = true;
                b.positive = true;
                diff = b - a;
                a.positive = false;
                b.positive = false;
                return diff;
            }
            else if (a.positive && b.positive)
            {
                if (b > a) //don't want to do something like 3 - 4. Instead do 4 - 3 and then multiply by -1.
                {
                    return -1 * (b - a);
                }
            }
            //3. Do the actual arithmetic. Know #a's digits >= #b's digits because of how we arranged things.
            int i = 0;
            bool borrowed = false;
            bool currA = false; //the value of a.binaryDigits[i] that we will use and alter
            while (i <= b.startIndex)
            {
                currA = a.binaryDigits[i];
                if (borrowed && currA) //When we "borrow" digits, even the score by removing them when we encounter a 1 next
                {
                    currA = false;
                    borrowed = false;
                }
                if (!currA && b.binaryDigits[i]) //Treat 0 - 1 as 1, but take away digits later on in the borrowing process.
                {
                    if (!borrowed)
                    {
                        diff.binaryDigits[i] = true; //if already borrowed set to 0
                        diff.startIndex = i;
                    }
                    borrowed = true;
                }
                //else if (currA == b.binaryDigits[i]) diff.binaryDigits[i] = false; //1-1 = 0-0 = 0. Don't need to include b/c 0 by def.
                else if (currA && !b.binaryDigits[i])
                {
                    diff.binaryDigits[i] = true; //1 - 0 = 1
                    diff.startIndex = i;
                }
                i++;
            }
            //If a was greater in digits, simply append the remaining digits from a while removing some if "borrowed"
            while (i <= a.startIndex) //will not run if a.startIndex == b.startIndex, b/c now i == b.startIndex + 1
            {
                currA = a.binaryDigits[i];
                if (borrowed && currA) currA = false;
                diff.binaryDigits[i] = currA;
                if (currA) diff.startIndex = i;
                i++;
            }
            //TESTING:
            printOperation(a, b, "-", diff);
            //END TESTING
            return diff;
        }

        #region Multiplication
        public static LargeInt operator *(LargeInt a, LargeInt b) //TODO: Implement this!!
        {
            //1. Calculate how many bits to use in the new LargeInt
            int newBits = a.bits() < b.bits() ? a.bits() : b.bits(); //have the default size as whichever one has less bits
            int combinedBits = a.usedBits() + b.usedBits();
            if (combinedBits > a.bits() && combinedBits > b.bits())
                newBits = 2 * combinedBits; //if result will be greater than either's available bits, result will have double the space
            else
            {
                if (b.bits() < a.bits() && combinedBits < a.bits() && combinedBits > b.bits()) // |b| < combinedBits < |a|
                    newBits = a.bits();
                else if (a.bits() < b.bits() && combinedBits < b.bits() && combinedBits > a.bits()) // |a| < combinedBits < |b|
                    newBits = b.bits();
                //else combinedBits < |a|, |b| so we just use the smaller of |a| and |b|
            }
            //2. Simple: Check if a or b are 0, 1, -1.
            LargeInt toReturn;
            if (a.startIndex == 0)
            {
                if (a.binaryDigits[a.startIndex])
                {
                    if (a.positive) //a is 1
                        return b;
                    else //-1
                    {
                        toReturn = new LargeInt(b);
                        toReturn.positive = !b.positive;
                        return toReturn;
                    }
                }
                else //a is 0
                    return a; //don't need to make copy b/c assignment op. will make one for us
            }
            else if (b.startIndex == 0)
            {
                if (b.binaryDigits[b.startIndex])
                {
                    if (b.positive) //b is 1
                        return a;
                    else //-1
                    {
                        toReturn = new LargeInt(a);
                        toReturn.positive = !a.positive;
                        return toReturn;
                    }
                }
                else //b is 0
                    return b;
            }
            //3. Neither are 0, 1, nor -1: not simple. Begin multiplication algorithm.
            toReturn = new LargeInt(newBits);
            toReturn.positive = (a.positive == b.positive); //Number is positive if multiplied by two pos or two negative numbers.
            return toReturn;
        }

        public static LargeInt operator *(LargeInt a, long b)
        {
            if (b == -1) //Simple sign change. But since we change a, must make a copy
            {
                LargeInt toReturn = new LargeInt(a);
                toReturn.positive = !a.positive;
                return toReturn;
            }
            else if (b == 1)
                return a; //Multiplicative identity. Don't need to make a copy b/c assignment operator will do it (LargeInt a = b * 1 will create a new instance for a)
            else
            {
                return a * (LargeInt)b; //nothing simple, so call the main multiplication function
            }
        }

        public static LargeInt operator *(long a, LargeInt b)
        {
            return b * a; //invoke the function with reversed arguments
        }
        #endregion

        //TODO: Implement this /
        public static LargeInt operator /(LargeInt a, LargeInt b)
        {
            return new LargeInt(); //REMOVE THIS
        }

        public static LargeInt operator ++(LargeInt a) { //not sure why anyone would use a LargeInt as a counter, but...
            return a + (LargeInt) 1;
        }

        public static LargeInt operator --(LargeInt a)
        {
            return a - (LargeInt) 1;
        }
        #endregion

        #region TYPE CONVERSION: LargeInt -> str ;  long, double, str, binaryString -> LargeInt
        public override string ToString() //return the binary representation
        {
            string toReturn = positive ? "" : "-"; //sign comes first
            for (int i = startIndex; i >= 0; i--) //least-significant digit is at index 0, so start at most significant, which is startIndex
            {
                toReturn += binaryDigits[i] ? "1" : "0";
            }
            return toReturn;
        }

        public static explicit operator LargeInt(long integer)  //Very useful: Explicitly converts a base-10 long integer into its binary representation
        {
            //1. Determine # of digits integer has in base 10
            int digits10 = integer.ToString().Length; //the number of decimal digits the unsigned version of integer has
            if (integer < 0) digits10--;
            //2. Determine # of digits the unsigned binary representation of integer will have. Why the formula below is valid:
            /*  1 decimal digit  =       9 max =                     1001 =  4 bits.  4 = 3*1 + 1
             *  2 decimal digits =      99 max =                  1100011 =  7 bits.  7 = 3*2 + 1
             *  3 decimal digits =     999 max =               1111100111 = 10 bits. 10 = 3*3 + 1
             *  4 decimal digits =    9999 max =           10011100001111 = 14 bits. 14 = 3*4 + 2
             *  5 decimal digits =   99999 max =        11000011010011111 = 17 bits. 17 = 3*5 + 2
             *  6 decimal digits =  999999 max =     11110100001000111111 = 20 bits. 20 = 3*6 + 2
             *  7 decimal digits = 9999999 max = 100110001001011001111111 = 24 bits. 24 = 3*7 + 3
             *  ...
             */
            //So the pattern is #bits = 3*(#decimalDigits) + ceil(#decimalDigits / 3).
            int digits2 = 3 * digits10 + (int)Math.Ceiling((double)(digits10 / 3));  //number of bits
            //2. Assign the LargeInt to have space of double the amount it will take up now to allow for growth
            LargeInt toReturn = new LargeInt(2 * digits2);
            //3. Check sign. If integer is negative, make it unsigned while we do calculation on it.
            toReturn.positive = (integer >= 0);
            if (integer < 0) integer *= -1; //we always want to work with the unsigned value here, else modular arithmetic will be funky
            //4. The important part: Convert unsigned value to binary
            /* Use rule that the n's place is 1 iff integer >= n (mod 2n).
             *    Ex: 21 mod 8 = 5. 21 in binary = 10101. 4's place is used.  */
            int powerOf2 = 0;
            //MessageBox.Show(intPow(2, powerOf2).ToString());
            while (integer >= intPow(2, powerOf2))
            {
                //MessageBox.Show("starting while loop iteration" + powerOf2);
                toReturn.binaryDigits[powerOf2] = (integer % intPow(2, powerOf2 + 1) >= powerOf2);
                if (integer % intPow(2, powerOf2 + 1) >= intPow(2, powerOf2))
                {
                    toReturn.binaryDigits[powerOf2] = true;

                }
                else toReturn.binaryDigits[powerOf2] = false;
                powerOf2++;
            }
            toReturn.startIndex = powerOf2 - 1;
            if (toReturn.startIndex == -1) toReturn.startIndex = 0; //means integer = 0
            //MessageBox.Show("startIndex/most significant digit place = " + toReturn.startIndex);
            return toReturn;
        }

        /// <summary>
        /// Explicitly converts a double to a LargeInt. Decimals will be truncated.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static explicit operator LargeInt(double num)
        {
            return (LargeInt) ((long)num);
        }

        /// <summary>
        /// Takes a string in signed base-10 format and uses it to construct a LargeInt. If the value is not an integer, decimals will be truncated.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        //TODO: This fxn
        public static LargeInt toLargeInt(string str) //very helpful: construct a LargeInt from a string. Assumes it's in base 10!
        {
            LargeInt toReturn = new LargeInt(/*fill in*/);
            char[] arr = str.ToCharArray();
            //Convert the string to int, while checking for invalid values. Accepted = '0'...'9', and '-' only at beginning
            //Then call intToLargeInt()
            return toReturn;
        }

        /// <summary>
        /// Takes a string in signed binary format and uses it to construct a LargeInt. Ex: "-1001", "11001".
        /// </summary>
        /// <param name="binaryStr"></param>
        /// <returns></returns>
        public static LargeInt binaryStringToLargeInt(string binaryStr) //not as common for the user, but far easier
        {
            LargeInt toReturn = new LargeInt(2 * binaryStr.Length); //have double the amount of space required, to allow for increases
            char[] arr = binaryStr.ToCharArray();
            int end = 0; //when to stop the for-loop iteration. 0 indicates positive, 1 indicates negative
            //Check the 0th char in the string for positive/negative:
            if (arr[0] == '-')
            {
                toReturn.positive = false;
                end = 1;
            }
            else
                toReturn.positive = true;
            //Now go through all the string (except for 0th char if negative) and put it into the LargeInt
            for (int i = arr.Length - 1; i >= end; i--) //start with least-significant digits first
            {
                if (arr[i] == '0') toReturn.binaryDigits[arr.Length - 1 - i] = false;
                else if (arr[i] == '1') toReturn.binaryDigits[arr.Length - 1 - i] = true;
                else throw new System.ArgumentException("Parameter must be a string representation of a binary number! It may only contain '0's and '1's, or one '-' at the beginning to indicate the number is negative.", "binaryStr");

            }
            toReturn.startIndex = arr.Length - 1 - end;
            return toReturn;
        }
        #endregion


        /// <summary>
        /// Returns a LargeInt constructed using the absolute value of the parameter.
        /// </summary>
        /// <param name="signedLargeInt"></param>
        /// <returns></returns>
        public static LargeInt abs(LargeInt signedLargeInt) /*Simple, but unfortunately O(n). 
        CANNOT just change sign of signedLargeInt and return b/c it is a pointer! We need to make a copy, which is always O(n). */
        {
            LargeInt toReturn = new LargeInt(signedLargeInt);
            toReturn.positive = true;
            return toReturn;
        }


        protected static void printOperation(LargeInt operand1, LargeInt operand2, string operation, LargeInt result) //for testing
        {
            string aStr = operand1.ToString(), bStr = operand2.ToString(), resultStr = result.ToString();
            //Find the length of the longest of these 3 strings:
            int longest = operand1.startIndex > operand2.startIndex ? operand1.startIndex : operand2.startIndex;
            longest = longest > result.startIndex ? longest : result.startIndex;
            //For much easier reading: Make sure that all 3 binary numbers have the same amount of printed digits
            for (int count = operand1.startIndex; count < longest; count++)
                aStr = "0" + aStr;
            for (int count = operand2.startIndex; count < longest; count++)
                bStr = "0" + bStr;
            for (int count = result.startIndex; count < longest; count++)
                resultStr = "0" + resultStr;
            MessageBox.Show(aStr + " " + operation + " " + Environment.NewLine + bStr + " = " + Environment.NewLine + resultStr);
        }

        #region Non-specific helper functions: intPow
        //Simple helper fxns not specifically having to do with LargeInt
        protected static long intPow(int num, int power) //computes num^power where power is an integer
        {
            if (num == 0 && power == 0) //Invalid case
            {
                MessageBox.Show("LargeInt.intPow(): ERROR: 0^0 is undefined!");
                return -1; //signify invalid
            }
            else if (power < 0)
            {
                MessageBox.Show("LargeInt.intPow(): ERROR: Only computes non-negative powers of integers!");
                return -1; //signify invalid
            }
            if (power == 0) return 1; //Base case 0: x^0 = 1
            return num * intPow(num, power - 1); //Recursive case
        }
        #endregion

    }
}
