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

namespace Spring.RabbitQuickStart.Common.Data
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class TradeResponse
    {
        private string ticker;

        private long quantity;

        private decimal price;

        private string orderType;

        private string confirmationNumber;

        private bool error;

        private string errorMessage;

        public string Ticker
        {
            get { return ticker; }
            set { ticker = value; }
        }

        public long Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        public string OrderType
        {
            get { return orderType; }
            set { orderType = value; }
        }

        public string ConfirmationNumber
        {
            get { return confirmationNumber; }
            set { confirmationNumber = value; }
        }

        public bool Error
        {
            get { return error; }
            set { error = value; }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }

        public bool Equals(TradeResponse other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.ticker, ticker) && other.quantity == quantity && other.price == price && Equals(other.orderType, orderType) && Equals(other.confirmationNumber, confirmationNumber) && other.error.Equals(error) && Equals(other.errorMessage, errorMessage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (TradeResponse)) return false;
            return Equals((TradeResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (ticker != null ? ticker.GetHashCode() : 0);
                result = (result*397) ^ quantity.GetHashCode();
                result = (result*397) ^ price.GetHashCode();
                result = (result*397) ^ (orderType != null ? orderType.GetHashCode() : 0);
                result = (result*397) ^ (confirmationNumber != null ? confirmationNumber.GetHashCode() : 0);
                result = (result*397) ^ error.GetHashCode();
                result = (result*397) ^ (errorMessage != null ? errorMessage.GetHashCode() : 0);
                return result;
            }
        }
    }

}