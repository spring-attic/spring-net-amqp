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
    /// Simple POCO class that represents a simple trade request
    /// </summary>
    /// <author>Mark Pollack</author>
    public class TradeRequest
    {
        private string ticker;

        private long quantity;

        private decimal price;

        private string orderType;

        private string accountName;

        private bool buyRequest;

        private string userName;

        private string requestId;

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

        public string AccountName
        {
            get { return accountName; }
            set { accountName = value; }
        }

        public bool BuyRequest
        {
            get { return buyRequest; }
            set { buyRequest = value; }
        }

        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        public string RequestId
        {
            get { return requestId; }
            set { requestId = value; }
        }

        public bool Equals(TradeRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.ticker, ticker) && other.quantity == quantity && other.price == price && Equals(other.orderType, orderType) && Equals(other.accountName, accountName) && other.buyRequest.Equals(buyRequest) && Equals(other.userName, userName) && Equals(other.requestId, requestId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (TradeRequest)) return false;
            return Equals((TradeRequest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (ticker != null ? ticker.GetHashCode() : 0);
                result = (result*397) ^ quantity.GetHashCode();
                result = (result*397) ^ price.GetHashCode();
                result = (result*397) ^ (orderType != null ? orderType.GetHashCode() : 0);
                result = (result*397) ^ (accountName != null ? accountName.GetHashCode() : 0);
                result = (result*397) ^ buyRequest.GetHashCode();
                result = (result*397) ^ (userName != null ? userName.GetHashCode() : 0);
                result = (result*397) ^ (requestId != null ? requestId.GetHashCode() : 0);
                return result;
            }
        }    
    }

}