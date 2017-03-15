using System;
using System.Collections.Generic;

namespace Explainer
{
    public class ResourceCache
    {
        public static float GetAbundance(string resourceName, Vessel vessel)
        {
            return GetAbundance(resourceName, vessel.mainBody, vessel.latitude, vessel.longitude);
        }
        public static float GetAbundance(string resourceName, CelestialBody body, double lat, double lon)
        {
            var biome = ScienceUtil.GetExperimentBiome(body, lat, lon);
            var key = new Key(resourceName, biome, lat, lon);
            if (!cache.ContainsKey(key))
            {
                var req = new AbundanceRequest
                {
                    Latitude = lat,
                    Longitude = lon,
                    BodyId = body.flightGlobalsIndex,
                    ResourceName = resourceName,
                    ResourceType = HarvestTypes.Planetary,
                    Altitude = 0,
                    CheckForLock = false,
                    BiomeName = biome,
                    ExcludeVariance = false,
                };
                cache[key] = ResourceMap.Instance.GetAbundance(req);
            }
            return cache[key];
        }
        internal class Key {
            public string resourceName;
            public string biome;
            public double lat;
            public double lon;
            public Key(string r, string b, double lat, double lon) { resourceName = r; biome = b; this.lat = lat; this.lon = lon; }
        }
        private static Dictionary<Key, float> cache = new Dictionary<Key, float>();
    }
}

