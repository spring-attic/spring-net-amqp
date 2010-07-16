#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

#region

using System;
using System.Collections.Generic;
using Erlang.NET;

#endregion

namespace Spring.Erlang.Support.Converter
{
    /// <summary>
    /// Converter that supports the basic types. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleErlangConverter : IErlangConverter
    {
        #region Implementation of IErlangConverter

        /// <summary>
        /// Convert a .NET object to a Erlang data type.
        /// </summary>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>the Erlang data type</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        public virtual OtpErlangObject ToErlang(object objectToConvert)
        {
            if (objectToConvert is OtpErlangObject)
            {
                return (OtpErlangObject) objectToConvert;
            }
            if (objectToConvert is object[])
            {
                object[] objectsToConvert = (object[]) objectToConvert;
                if (objectsToConvert.Length != 0)
                {
                    List<OtpErlangObject> tempList = new List<OtpErlangObject>();
                    foreach (object toConvert in objectsToConvert)
                    {
                        OtpErlangObject erlangObject = ConvertBasicTypeToErlang(toConvert);
                        tempList.Add(erlangObject);
                    }
                    OtpErlangObject[] ia = tempList.ToArray();
                    return new OtpErlangList(ia);
                }
                else
                {
                    return new OtpErlangList();
                }
            }
            else
            {
                return ConvertBasicTypeToErlang(objectToConvert);
            }
        }

        private OtpErlangObject ConvertBasicTypeToErlang(object obj)
        {
            if (obj is byte[])
            {
                return new OtpErlangBinary((byte[]) obj);
            }
            else if (obj is Boolean)
            {
                return new OtpErlangBoolean((Boolean) obj);
            }
            else if (obj is Byte)
            {
                return new OtpErlangByte((Byte) obj);
            }
            else if (obj is Char)
            {
                return new OtpErlangChar((Char) obj);
            }
            else if (obj is Double)
            {
                return new OtpErlangDouble((Double) obj);
            }
            else if (obj is float)
            {
                return new OtpErlangFloat((float) obj);
            }
            else if (obj is int)
            {
                return new OtpErlangInt((int) obj);
            }
            else if (obj is long)
            {
                return new OtpErlangLong((long) obj);
            }
            else if (obj is short)
            {
                return new OtpErlangShort((short) obj);
            }
            else if (obj is String)
            {
                return new OtpErlangString((String) obj);
            }
            else
            {
                throw new ErlangConversionException(
                    "Could not convert .NET object of type [" + obj.GetType()
                    + "] to an Erlang data type.");
            }
        }

        /// <summary>
        /// Convert from an Erlang data type to a .NET data type.
        /// </summary>
        /// <param name="erlangObject">The erlang object.</param>
        /// <returns>The converted .NET object</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        public virtual object FromErlang(OtpErlangObject erlangObject)
        {
            //TODO support arrays
            return ConvertErlangToBasicType(erlangObject);
        }

        private object ConvertErlangToBasicType(OtpErlangObject erlangObject)
        {
            try
            {
                if (erlangObject is OtpErlangBinary)
                {
                    return ((OtpErlangBinary) erlangObject).binaryValue();
                }
                else if (erlangObject is OtpErlangAtom)
                {
                    return ((OtpErlangAtom) erlangObject).atomValue();
                }
                else if (erlangObject is OtpErlangBinary)
                {
                    return ((OtpErlangBinary) erlangObject).binaryValue();
                }
                else if (erlangObject is OtpErlangBoolean)
                {
                    return ExtractBoolean(erlangObject);
                }
                else if (erlangObject is OtpErlangByte)
                {
                    return ((OtpErlangByte) erlangObject).byteValue();
                }
                else if (erlangObject is OtpErlangChar)
                {
                    return ((OtpErlangChar) erlangObject).charValue();
                }
                else if (erlangObject is OtpErlangDouble)
                {
                    return ((OtpErlangDouble) erlangObject).doubleValue();
                }
                else if (erlangObject is OtpErlangFloat)
                {
                    return ((OtpErlangFloat) erlangObject).floatValue();
                }
                else if (erlangObject is OtpErlangInt)
                {
                    return ((OtpErlangInt) erlangObject).intValue();
                }
                else if (erlangObject is OtpErlangLong)
                {
                    return ((OtpErlangLong) erlangObject).longValue();
                }
                else if (erlangObject is OtpErlangShort)
                {
                    return ((OtpErlangShort) erlangObject).shortValue();
                }
                else if (erlangObject is OtpErlangString)
                {
                    return ((OtpErlangString) erlangObject).stringValue();
                }
                else if (erlangObject is OtpErlangPid)
                {
                    return (erlangObject).ToString();
                }
                else
                {
                    throw new ErlangConversionException(
                        "Could not convert Erlang object ["
                        + erlangObject.GetType() + "] to .NET type.");
                }
            }
            catch (OtpErlangRangeException ex)
            {
                //TODO Erlang.NET exceptions do not support nesting root exceptions.
                throw new ErlangConversionException("Could not convert Erlang object [" + erlangObject.GetType()
                                                    + "] to .NET type.  OtpErlangRangeException msg [" + ex.Message +
                                                    "]");
            }
        }

        /// <summary>
        /// The return value from executing the Erlang RPC.
        /// </summary>
        /// <param name="module">The module to call</param>
        /// <param name="function">The function to invoke</param>
        /// <param name="erlangObject">The erlang object that is passed in as a parameter</param>
        /// <returns>The converted .NET object return value from the RPC call.</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception> 
        public virtual object FromErlangRpc(string module, string function, OtpErlangObject erlangObject)
        {
            return FromErlang(erlangObject);
        }

        #endregion

        public static bool ExtractBoolean(OtpErlangObject erlangObject)
        {
            // TODO Erlang.NET has wrong capitilization
            return ((OtpErlangBoolean) erlangObject).boolValue();
        }

        public static String ExtractPid(OtpErlangObject value)
        {
            return (value).ToString();
        }

        public static long ExtractLong(OtpErlangObject value)
        {
            return ((OtpErlangLong) value).longValue();
        }
    }
}