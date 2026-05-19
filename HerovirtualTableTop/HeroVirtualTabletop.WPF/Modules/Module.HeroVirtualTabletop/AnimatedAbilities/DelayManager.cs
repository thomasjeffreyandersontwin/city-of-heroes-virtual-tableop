using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class DelayManager
    {
        private Dictionary<double, double> distanceDelayMappingDictionary;

        private PauseElement pauseElement;

        public DelayManager(PauseElement pauseElement)
        {
            this.pauseElement = pauseElement;
            this.ConstructDelayDictionary();
        }

        private void ConstructDelayDictionary()
        {
            distanceDelayMappingDictionary = new Dictionary<double, double>();
            distanceDelayMappingDictionary.Add(10, pauseElement.CloseDistanceDelay);
            distanceDelayMappingDictionary.Add(20, pauseElement.ShortDistanceDelay);
            distanceDelayMappingDictionary.Add(50, pauseElement.MediumDistanceDelay);
            distanceDelayMappingDictionary.Add(100, pauseElement.LongDistanceDelay);
            distanceDelayMappingDictionary.Add(15, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[10], distanceDelayMappingDictionary[20], 0.70));
            distanceDelayMappingDictionary.Add(30, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[20], distanceDelayMappingDictionary[50], 0.6));
            distanceDelayMappingDictionary.Add(40, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[20], distanceDelayMappingDictionary[50], 0.875));
            distanceDelayMappingDictionary.Add(60, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.4));
            distanceDelayMappingDictionary.Add(70, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.5));
            distanceDelayMappingDictionary.Add(80, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.7));
            distanceDelayMappingDictionary.Add(90, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.87));
        }

        private double GetPercentageDelayBetweenTwoDelays(double firstDelay, double secondDelay, double percentage)
        {
            return firstDelay - (firstDelay - secondDelay) * percentage;
        }

        private double GetLinearDelayBetweenTwoDelays(double firstDistance, double firstDelay, double secondDistance, double secondDelay, double targetDistance)
        {
            // y - y1 = m(x - x1); m = (y2 - y1)/(x2 - x1)
            var m = (secondDelay - firstDelay) / (secondDistance - firstDistance);
            double targetDelay = firstDelay + m * (targetDistance - firstDistance);
            return targetDelay;
        }

        public double GetDelayForDistance(double distance)
        {
            double targetDelay = 0;
            if(distanceDelayMappingDictionary.ContainsKey(distance))
            {
                targetDelay = distanceDelayMappingDictionary[distance];
            }
            else if (distance <= 10)
            {
                targetDelay = distanceDelayMappingDictionary[10];
            }
            else if (distance < 100)
            {
                double nearestLowerDistance = distanceDelayMappingDictionary.Keys.OrderBy(d => d).Last(d => d < distance);
                double nearestHigherDistance = distanceDelayMappingDictionary.Keys.OrderBy(d => d).First(d => d > distance);
                targetDelay = GetLinearDelayBetweenTwoDelays(nearestLowerDistance, distanceDelayMappingDictionary[nearestLowerDistance], nearestHigherDistance, distanceDelayMappingDictionary[nearestHigherDistance], distance);
            }
            else
            {
                double baseDelayDiff = distanceDelayMappingDictionary[50] - distanceDelayMappingDictionary[100];
                double baseDelay = distanceDelayMappingDictionary[100];
                int nearestLowerHundredMultiplier = (int)(distance / 100);
                int nearestHigherHundredMultiplier = nearestLowerHundredMultiplier + 1;
                double nearestLowerHundredDistance = nearestLowerHundredMultiplier * 100;
                double nearestHigherHundredDistance = nearestHigherHundredMultiplier * 100;
                double currentLowerDelay = baseDelay;
                double currentHigherDelay = baseDelay - baseDelayDiff * 0.5;
                for (int i = 1; i < nearestLowerHundredMultiplier; i++)
                {
                    baseDelayDiff = currentLowerDelay - currentHigherDelay;
                    currentLowerDelay = currentHigherDelay;
                    currentHigherDelay = currentLowerDelay - baseDelayDiff * 0.5;
                }
                targetDelay = GetLinearDelayBetweenTwoDelays(nearestLowerHundredDistance, currentLowerDelay, nearestHigherHundredDistance, currentHigherDelay, distance);
            }
            double targetDistance = distance < 10 ? 10 : distance;
            return targetDelay * targetDistance;
        }
    }
}
