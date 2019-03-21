// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleTrade.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>The simple trade.</summary>
    public class SimpleTrade
    {
        private string ticker;

        private ulong quantity;

        private decimal price;

        private string orderType;

        private string accountName;

        private bool buyRequest;

        private string userName;

        private string requestId;

        /// <summary>Gets or sets the ticker.</summary>
        public string Ticker { get { return this.ticker; } set { this.ticker = value; } }

        /// <summary>Gets or sets the quantity.</summary>
        public ulong Quantity { get { return this.quantity; } set { this.quantity = value; } }

        /// <summary>Gets or sets the price.</summary>
        public decimal Price { get { return this.price; } set { this.price = value; } }

        /// <summary>Gets or sets the order type.</summary>
        public string OrderType { get { return this.orderType; } set { this.orderType = value; } }

        /// <summary>Gets or sets the account name.</summary>
        public string AccountName { get { return this.accountName; } set { this.accountName = value; } }

        /// <summary>Gets or sets a value indicating whether is buy request.</summary>
        public bool IsBuyRequest { get { return this.buyRequest; } set { this.buyRequest = value; } }

        /// <summary>Gets or sets the user name.</summary>
        public string UserName { get { return this.userName; } set { this.userName = value; } }

        /// <summary>Gets or sets the request id.</summary>
        public string RequestId { get { return this.requestId; } set { this.requestId = value; } }

        /// <summary>The get hash code.</summary>
        /// <returns>The System.Int32.</returns>
        public override int GetHashCode()
        {
            const int prime = 31;
            var result = 1;
            result = prime * result + ((this.accountName == null) ? 0 : this.accountName.GetHashCode());
            result = prime * result + (this.buyRequest ? 1231 : 1237);
            result = prime * result + ((this.orderType == null) ? 0 : this.orderType.GetHashCode());
            result = prime * result + ((this.price == null) ? 0 : this.price.GetHashCode());
            result = prime * result + (int)(this.quantity ^ (this.quantity >> 32));
            result = prime * result + ((this.requestId == null) ? 0 : this.requestId.GetHashCode());
            result = prime * result + ((this.ticker == null) ? 0 : this.ticker.GetHashCode());
            result = prime * result + ((this.userName == null) ? 0 : this.userName.GetHashCode());
            return result;
        }

        /// <summary>The equals.</summary>
        /// <param name="obj">The obj.</param>
        /// <returns>The System.Boolean.</returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (typeof(SimpleTrade) != obj.GetType())
            {
                return false;
            }

            var other = (SimpleTrade)obj;
            if (this.accountName == null)
            {
                if (other.accountName != null)
                {
                    return false;
                }
            }
            else if (!this.accountName.Equals(other.accountName))
            {
                return false;
            }

            if (this.buyRequest != other.buyRequest)
            {
                return false;
            }

            if (this.orderType == null)
            {
                if (other.orderType != null)
                {
                    return false;
                }
            }
            else if (!this.orderType.Equals(other.orderType))
            {
                return false;
            }

            if (this.price == null)
            {
                if (other.price != null)
                {
                    return false;
                }
            }
            else if (!this.price.Equals(other.price))
            {
                return false;
            }

            if (this.quantity != other.quantity)
            {
                return false;
            }

            if (this.requestId == null)
            {
                if (other.requestId != null)
                {
                    return false;
                }
            }
            else if (!this.requestId.Equals(other.requestId))
            {
                return false;
            }

            if (this.ticker == null)
            {
                if (other.ticker != null)
                {
                    return false;
                }
            }
            else if (!this.ticker.Equals(other.ticker))
            {
                return false;
            }

            if (this.userName == null)
            {
                if (other.userName != null)
                {
                    return false;
                }
            }
            else if (!this.userName.Equals(other.userName))
            {
                return false;
            }

            return true;
        }
    }
}
