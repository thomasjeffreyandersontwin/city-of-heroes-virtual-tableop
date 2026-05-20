using Module.Shared;
using Framework.WPF.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.OptionGroups;
using System.Reflection;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Characters;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    public class Helper
    {
        #region Global Variables for the application
        public static object GlobalClipboardObject
        {
            get;
            set;
        }

        public static object GlobalClipboardObjectParent
        {
            get;
            set;
        }

        public static ClipboardAction GlobalClipboardAction
        {
            get;
            set;
        }

        public static List<AnimatedAbility> GlobalDefaultAbilities
        {
            get;
            set;
        }
        public static List<AnimatedAbility> GlobalCombatAbilities
        {
            get;
            set;
        }

        public static List<CharacterMovement> GlobalMovements
        {
            get;
            set;
        }

        public static bool GlobalVariables_IsPlayingAttack
        {
            get;
            set;
        }

        public static AnimatedAbility GlobalDefaultSweepAbility
        {
            get;
            set;
        }

        public static System.Windows.Point GlobalVariables_OptionGroupDragStartPoint { get; set; }
        public static string GlobalVariables_DraggingOptionGroupName { get; set; }

        public static string GlobalVariables_CurrentActiveWindowName { get; set; }

        public static CharacterMovement GlobalVariables_CharacterMovement { get; set; }

        public static CharacterMovement GlobalVariables_FormerActiveCharacterMovement { get; set; }

        public static Character GlobalVariables_ActiveCharacter { get; set; }

        public static Dictionary<string, object> GlobalVariables_UISettings = new Dictionary<string, object>();

        public static bool GlobalVariables_IntegrateWithHCS = false;

        #endregion

        #region Resource Dictionary and Style related
        public static System.Windows.Style GetCustomStyle(string styleName)
        {
            System.Windows.ResourceDictionary resource = new System.Windows.ResourceDictionary
            {
                Source = new Uri(Constants.RESOURCE_DICTIONARY_PATH, UriKind.RelativeOrAbsolute)
            };
            return (System.Windows.Style)resource[styleName];
        }

        public static System.Windows.Style GetCustomWindowStyle()
        {
            return GetCustomStyle(Constants.CUSTOM_MODELESS_TRANSPARENT_WINDOW_STYLENAME);
        }

        #endregion

        #region JSON Serialize/Deserialize

        // Caches Type.GetType() results so Newtonsoft doesn't re-scan assemblies for every $type token.
        // With thousands of objects of the same types, this converts O(N*assemblies) to O(N) dictionary hits.
        private class CachingSerializationBinder : Newtonsoft.Json.Serialization.ISerializationBinder
        {
            private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();
            private static readonly object Lock = new object();

            public Type BindToType(string assemblyName, string typeName)
            {
                string key = assemblyName + "\0" + typeName;
                Type t;
                lock (Lock) { if (Cache.TryGetValue(key, out t)) return t; }

                string fullName = string.IsNullOrEmpty(assemblyName) ? typeName : typeName + ", " + assemblyName;
                t = Type.GetType(fullName);
                if (t == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = asm.GetType(typeName);
                        if (t != null) break;
                    }
                }
                lock (Lock) { Cache[key] = t; }
                return t;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
            }
        }

        private static readonly CachingSerializationBinder SharedBinder = new CachingSerializationBinder();
        public static T GetDeserializedJSONFromFile<T>(string fileName)
        {
            T obj = default(T);
            if (!File.Exists(fileName))
            {
                CreateFile(fileName);
                return obj;
            }
            JsonSerializer serializer = new JsonSerializer();
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, 65536))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                serializer.SerializationBinder = SharedBinder;
                obj = serializer.Deserialize<T>(reader);
            }
            return obj;
        }

        public static void SerializeObjectAsJSONToFile<T>(string fileName, T obj)
        {
            // Write to a temp file first; rename over the target only after the write succeeds.
            // This prevents a force-kill mid-write from zeroing the live data file.
            string tempFile = fileName + ".tmp";
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(tempFile))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;
                    serializer.Serialize(writer, obj);
                }
                File.Copy(tempFile, fileName, overwrite: true);
                File.Delete(tempFile);
            }
            catch (Exception)
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                throw;
            }
        }

        private static JsonSerializer BuildCrowdSerializer()
        {
            return new JsonSerializer
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Objects
                // No Formatting.Indented — compact JSON shrinks the cache file significantly
            };
        }

        // Loads from a GZip-compressed compact-JSON cache file.
        public static T GetDeserializedJSONFromCacheFile<T>(string cacheFile)
        {
            using (var fs = new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            using (var gzip = new GZipStream(fs, CompressionMode.Decompress))
            using (var sr = new StreamReader(gzip, Encoding.UTF8))
            using (var reader = new JsonTextReader(sr))
            {
                var s = BuildCrowdSerializer();
                s.SerializationBinder = SharedBinder;
                return s.Deserialize<T>(reader);
            }
        }

        // Writes a GZip-compressed compact-JSON cache file (temp-then-rename for safety).
        public static void SerializeObjectAsCacheFile<T>(string cacheFile, T obj)
        {
            string tempFile = cacheFile + ".tmp";
            try
            {
                var serializer = BuildCrowdSerializer();
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                using (var gzip = new GZipStream(fs, CompressionLevel.Fastest))
                using (var sw = new StreamWriter(gzip, Encoding.UTF8))
                using (var writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, obj);
                }
                File.Copy(tempFile, cacheFile, overwrite: true);
                File.Delete(tempFile);
            }
            catch
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        #endregion

        #region File I/O
        public static void CreateFile(string fileName)
        {
            FileStream fs = File.Create(fileName);
            fs.Dispose();
        }

        #endregion

        #region TreeView

        public static object GetCurrentSelectedCrowdInCrowdCollection(Object tv, out ICrowdMemberModel crowdMember)
        {
            CrowdModel containingCrowdModel = null;
            crowdMember = null;
            TreeView treeView = tv as TreeView;

            if (treeView != null && treeView.SelectedItem != null)
            {
                if (treeView.SelectedItem is CrowdModel)
                {
                    containingCrowdModel = treeView.SelectedItem as CrowdModel;
                }
                else
                {
                    DependencyObject dObject = treeView.GetItemFromSelectedObject(treeView.SelectedItem);
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    crowdMember = tvi.DataContext as ICrowdMemberModel;
                    dObject = VisualTreeHelper.GetParent(tvi); // got the immediate parent
                    tvi = dObject as TreeViewItem; // now get first treeview item parent
                    while (tvi == null)
                    {
                        dObject = VisualTreeHelper.GetParent(dObject);
                        tvi = dObject as TreeViewItem;
                    }
                    containingCrowdModel = tvi.DataContext as CrowdModel;
                }
            }

            return containingCrowdModel;
        }

        public static object GetCurrentSelectedAnimationInAnimationCollection(Object tv, out IAnimationElement animationElement)
        {
            IAnimationElement selectedAnimationElement = null;
            animationElement = null;
            TreeView treeView = tv as TreeView;

            if (treeView != null && treeView.SelectedItem != null)
            {
                DependencyObject dObject = treeView.GetItemFromSelectedObject(treeView.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if (tvi != null)
                    selectedAnimationElement = tvi.DataContext as IAnimationElement;
                dObject = VisualTreeHelper.GetParent(tvi); // got the immediate parent
                tvi = dObject as TreeViewItem; // now get first treeview item parent
                while (tvi == null)
                {
                    dObject = VisualTreeHelper.GetParent(dObject);
                    tvi = dObject as TreeViewItem;
                    if (tvi == null)
                    {
                        var tView = dObject as TreeView;
                        if (tView != null)
                            break;
                    }
                    else
                        animationElement = tvi.DataContext as IAnimationElement;
                }
            }

            return selectedAnimationElement;
        }

        public static string GetTextFromControlObject(object control)
        {
            string text = null;
            PropertyInfo propertyInfo = control.GetType().GetProperty("Text");
            if (propertyInfo != null)
            {
                text = propertyInfo.GetValue(control).ToString();
            }
            return text;
        }

        #endregion

        #region ListBox

        public static object GetCurrentSelectedOptionInOptionGroup(Object lb)
        {
            ICharacterOption option = null;
            ListBox listBox = lb as ListBox;

            if (listBox != null && listBox.SelectedItem != null)
            {
                if (listBox.SelectedItem is ICharacterOption)
                {
                    option = listBox.SelectedItem as ICharacterOption;
                }
            }

            return option;
        }

        #endregion

        #region General Control

        public static Visual GetAncestorByType(DependencyObject element, Type type)
        {
            while (element != null && !(element.GetType() == type))
                element = VisualTreeHelper.GetParent(element);

            return element as Visual;
        }

        public static Visual GetTemplateAncestorByType(DependencyObject element, Type type)
        {
            while (element != null && !(element.GetType() == type))
                element = (element as FrameworkElement).TemplatedParent;

            return element as Visual;
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null) return null;
            if (element.GetType() == type) return element;
            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();
            for (int i = 0;
                i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        public static string GetContainerWindowName(object element)
        {
            Window win = null;
            string winName = "";

            if (element is Window)
            {
                win = element as Window;
                winName = win.Name;
            }
            else
            {
                DependencyObject dObj = element as DependencyObject;
                while (win == null)
                {
                    FrameworkElement elem = dObj as FrameworkElement;
                    dObj = elem.Parent;
                    if (dObj is Window)
                    {
                        win = dObj as Window;
                        winName = win.Name;
                        break;
                    }
                }
            }

            return winName;
        }

        // Helper to search up the VisualTree
        public static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        #endregion

        #region Collision Info
        //X:[126.30] Y:[-0.50] Z:[-60.09] D:[0.00]
        public static Vector3 GetCollisionVector(string collisionInfo)
        {
            float X = 0f, Y = 0f, Z = 0f;
            try
            {

                int indexXStart = collisionInfo.IndexOf("[");
                int indexXEnd = collisionInfo.IndexOf("]");
                string xStr = collisionInfo.Substring(indexXStart + 1, indexXEnd - indexXStart - 1);
                float.TryParse(xStr, out X);

                int indexYStart = collisionInfo.IndexOf("[", indexXEnd);
                int indexYEnd = collisionInfo.IndexOf("]", indexYStart);
                string yStr = collisionInfo.Substring(indexYStart + 1, indexYEnd - indexYStart - 1);
                float.TryParse(yStr, out Y);

                int indexZStart = collisionInfo.IndexOf("[", indexYEnd);
                int indexZEnd = collisionInfo.IndexOf("]", indexZStart);
                string zStr = collisionInfo.Substring(indexZStart + 1, indexZEnd - indexZStart - 1);
                float.TryParse(zStr, out Z);

                if (float.IsNaN(X))
                    X = 0;
                if (float.IsNaN(Y))
                    Y = 0;
                if (float.IsNaN(Z))
                    Z = 0;
            }
            catch (Exception ex)
            {
            }

            return new Vector3(X, Y, Z);
        }

        #endregion

        #region Movement

        public static MovementDirection GetMovementDirectionFromKey(Key key)
        {
            MovementDirection movementDirection = MovementDirection.None;
            switch (key)
            {
                case Key.A:
                    movementDirection = MovementDirection.Left;
                    break;
                case Key.W:
                    movementDirection = MovementDirection.Forward;
                    break;
                case Key.S:
                    movementDirection = MovementDirection.Backward;
                    break;
                case Key.D:
                    movementDirection = MovementDirection.Right;
                    break;
                case Key.Space:
                    movementDirection = MovementDirection.Upward;
                    break;
                case Key.Z:
                    movementDirection = MovementDirection.Downward;
                    break;
                case Key.X:
                    movementDirection = MovementDirection.Still;
                    break;

            }
            return movementDirection;
        }

        #endregion

        # region Vector Maths

        public static float Get2DAngleBetweenVectors(Vector3 v1, Vector3 v2, out bool isClockwiseTurn)
        {
            var x = v1.X * v2.Z - v2.X * v1.Z;
            isClockwiseTurn = x < 0;
            var dotProduct = Vector3.Dot(v1, v2);
            if (dotProduct > 1)
                dotProduct = 1;
            if (dotProduct < -1)
                dotProduct = -1;
            var y = (float)Math.Acos(dotProduct);
            return y;
        }

        public static Vector3 GetRoundedVector(Vector3 vector, int decimalPlaces)
        {
            float x = (float)Math.Round(vector.X, decimalPlaces);
            float y = (float)Math.Round(vector.Y, decimalPlaces);
            float z = (float)Math.Round(vector.Z, decimalPlaces);

            return new Vector3(x, y, z);
        }

        public static double GetRadianAngle(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public static Vector3 GetAdjacentPoint(Vector3 currentPositionVector, Vector3 facingVector, bool isLeft, float unitsToAdjacent = 2.5f)
        {
            Double rotationAngle = isLeft ? -90 : 90;
            MovementDirection direction = isLeft ? MovementDirection.Left : MovementDirection.Right;
            Vector3 directionVector = GetDirectionVector(rotationAngle, direction, facingVector);
            Vector3 destinationVector = GetDestinationVector(directionVector, unitsToAdjacent, currentPositionVector);
            return destinationVector;
        }

        public static bool DetermineIfOneObjectIsInFrontOfAnotherObject(Vector3 sourceObjectPositionVector, Vector3 sourceObjectFacingVector, Vector3 targetObjectPositionVector)
        {
            bool inFront = true;
            Vector3 directionVectorFromSourceToTarget = targetObjectPositionVector - sourceObjectPositionVector;
            directionVectorFromSourceToTarget.Normalize();
            sourceObjectFacingVector.Normalize();
            float dotProduct = Vector3.Dot(directionVectorFromSourceToTarget, sourceObjectFacingVector);
            if (dotProduct < 0)
                inFront = false;
            return inFront; 
        }

        public static Vector3 GetDirectionVector(double rotationAngle, MovementDirection direction, Vector3 facingVector)
        {
            float vX, vY, vZ;
            double rotationAxisX = 0, rotationAxisY = 1, rotationAxisZ = 0;
            if (direction == MovementDirection.Upward)
            {
                vX = 0;
                vY = 1;
                vZ = 0;
            }
            else if (direction == MovementDirection.Downward)
            {
                vX = 0;
                vY = -1;
                vZ = 0;
            }
            else

            {
                double rotationAngleRadian = Helper.GetRadianAngle(rotationAngle);
                double tr = 1 - Math.Sin(rotationAngleRadian);
                //a1 = (t(r) * X * X) + cos(r)
                var a1 = tr * rotationAxisX * rotationAxisX + Math.Cos(rotationAngleRadian);
                //a2 = (t(r) * X * Y) - (sin(r) * Z)
                var a2 = tr * rotationAxisX * rotationAxisY - Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //a3 = (t(r) * X * Z) + (sin(r) * Y)
                var a3 = tr * rotationAxisX * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisY;
                //b1 = (t(r) * X * Y) + (sin(r) * Z)
                var b1 = tr * rotationAxisX * rotationAxisY + Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //b2 = (t(r) * Y * Y) + cos(r)
                var b2 = tr * rotationAxisY * rotationAxisY + Math.Cos(rotationAngleRadian);
                //b3 = (t(r) * Y * Z) - (sin(r) * X)
                var b3 = tr * rotationAxisY * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c1 = (t(r) * X * Z) - (sin(r) * Y)
                var c1 = tr * rotationAxisX * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisY;
                //c2 = (t(r) * Y * Z) + (sin(r) * X)
                var c2 = tr * rotationAxisY * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c3 = (t(r) * Z * Z) + cos (r)
                var c3 = tr * rotationAxisZ * rotationAxisZ + Math.Cos(rotationAngleRadian);


                Vector3 facingVectorToDestination = facingVector;
                vX = (float)(a1 * facingVectorToDestination.X + a2 * facingVectorToDestination.Y + a3 * facingVectorToDestination.Z);
                vY = (float)(b1 * facingVectorToDestination.X + b2 * facingVectorToDestination.Y + b3 * facingVectorToDestination.Z);
                vZ = (float)(c1 * facingVectorToDestination.X + c2 * facingVectorToDestination.Y + c3 * facingVectorToDestination.Z);
            }

            return Helper.GetRoundedVector(new Vector3(vX, vY, vZ), 2);
        }

        public static Vector3 GetDestinationVector(Vector3 directionVector, float units, Character target)
        {
            return GetDestinationVector(directionVector, units, target.CurrentPositionVector);
        }

        public static Vector3 GetDestinationVector(Vector3 directionVector, float units, Vector3 positionVector)
        {
            Vector3 vCurrent = positionVector;
            directionVector.Normalize();
            var destX = vCurrent.X + directionVector.X * units;
            var destY = vCurrent.Y + directionVector.Y * units;
            var destZ = vCurrent.Z + directionVector.Z * units;
            Vector3 dest = new Vector3(destX, destY, destZ);
            dest = Helper.GetRoundedVector(dest, 2);
            return dest;
        }

        public static bool IsPointWithinQuadraticRegion(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD, Vector3 pointX)
        {
            // Following considers 3d
            Vector3 lineAB = pointB - pointA;
            Vector3 lineAC = pointC - pointA;
            Vector3 lineAX = pointX - pointA;
            float AXdotAB = Vector3.Dot(lineAX, lineAB);
            float ABdotAB = Vector3.Dot(lineAB, lineAB);
            float AXdotAC = Vector3.Dot(lineAX, lineAC);
            float ACdotAC = Vector3.Dot(lineAC, lineAC);

#if DEBUG
            if (AXdotAB == 0f || AXdotAC == 0f)
            {
                throw new Exception("Boundary case found for obstacle collision!");
            }
#endif
            return (0 < AXdotAB && AXdotAB < ABdotAB) && (0 < AXdotAC && AXdotAC < ACdotAC);
            //// Following considers 2d
            //Point a = new Point((int)pointA.X, (int)pointA.Z);
            //Point b = new Point((int)pointB.X, (int)pointB.Z);
            //Point c = new Point((int)pointC.X, (int)pointC.Z);
            //Point d = new Point((int)pointD.X, (int)pointD.Z);
            //Point p = new Point((int)pointX.X, (int)pointX.Z);

            //return IsPointInPolygon(p, new Point[] { a, b, c, d});
        }

        public static bool IsPointInPolygon(System.Windows.Point p, System.Windows.Point[] polygon)
        {
            double minX = polygon[0].X;
            double maxX = polygon[0].X;
            double minY = polygon[0].Y;
            double maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Length; i++)
            {
                System.Windows.Point q = polygon[i];
                minX = Math.Min(q.X, minX);
                maxX = Math.Max(q.X, maxX);
                minY = Math.Min(q.Y, minY);
                maxY = Math.Max(q.Y, maxY);
            }

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            {
                return false;
            }

            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                     p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        static public bool isLyingInCone(Vector3 apexVector, Vector3 baseCircleCenterVector, Vector3 pointToConsiderVector,
                                    float aperture)
        {

            // This is for our convenience
            float halfAperture = aperture / 2f;

            // Vector pointing to X point from apex
            Vector3 apexToXVect = apexVector - pointToConsiderVector;

            // Vector pointing from apex to circle-center point.
            Vector3 axisVect = apexVector - baseCircleCenterVector;

            // X is lying in cone only if it's lying in 
            // infinite version of its cone -- that is, 
            // not limited by "round basement".
            // We'll use dotProd() to 
            // determine angle between apexToXVect and axis.
            bool isInInfiniteCone = Vector3.Dot(apexToXVect, axisVect)
                                       /apexToXVect.Length() / axisVect.Length()
                                         >
                                       // We can safely compare cos() of angles 
                                       // between vectors instead of bare angles.
                                       Math.Cos(halfAperture);


            if (!isInInfiniteCone) return false;

            // X is contained in cone only if projection of apexToXVect to axis
            // is shorter than axis. 
            // We'll use dotProd() to figure projection length.
            bool isUnderRoundCap = Vector3.Dot(apexToXVect, axisVect)
                                      / axisVect.Length()
                                        <
                                      axisVect.Length();
            return isUnderRoundCap;
        }

        public static Vector3 GetIntersectionPointOfPerpendicularProjectionVectorOnAnotherVector(Vector3 referenceVector, Vector3 projectingVector)
        {
            Vector3 p1 = Vector3.Zero;//new Vector3(x1, y1, z1);
            //Vector3 p2 = new Vector3(x2, y2, z2);
            Vector3 q = projectingVector;

            Vector3 u = referenceVector;
            Vector3 pq = q - p1;
            Vector3 w2 = pq - Vector3.Multiply(u, Vector3.Dot(pq, u) / u.LengthSquared());

            Vector3 point = q - w2;
            return point;
        }

        public static double CalculateMaximumDistanceBetweenTwoPointsInASetOfPoints(params Vector3[] points)
        {
            float maxDistance = 0f;
            Vector3 maxDistancePoint = Vector3.Zero;
            var firstPoint = points.First();
            foreach(Vector3 otherPoint in points.Where(p => p != firstPoint))
            {
                float dist = Vector3.Distance(otherPoint, firstPoint);
                if(dist > maxDistance)
                {
                    maxDistance = dist;
                    maxDistancePoint = otherPoint;
                }
            }
            if(maxDistancePoint != Vector3.Zero)
            {
                foreach (Vector3 otherPoint in points.Where(p => p != maxDistancePoint))
                {
                    float dist = Vector3.Distance(otherPoint, maxDistancePoint);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        maxDistancePoint = otherPoint;
                    }
                }
            }
            return Math.Round(maxDistance, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region UI Settings

        public static void SaveUISettings(string settingsName, object value)
        {
            if (GlobalVariables_UISettings.ContainsKey(settingsName))
                GlobalVariables_UISettings[settingsName] = value;
            else
                GlobalVariables_UISettings.Add(settingsName, value);
        }

        public static object GetUISettings(string settingsName)
        {
            if (GlobalVariables_UISettings.ContainsKey(settingsName))
                return GlobalVariables_UISettings[settingsName];
            else
                return null;
        }

        #endregion

        #region Misc

        public static bool IsNumeric(object value)
        {
            int k;
            if (value != null && Int32.TryParse(value.ToString(), out k))
                return true;
            return false;
        }

        // Compiled once — avoids per-call Regex allocation during thousands of sort comparisons
        private static readonly Regex CompareStringsRegex = new Regex(@"^(.*?)(\d+)(\D*)$", RegexOptions.Compiled);

        public static int CompareStrings(string s1, string s2)
        {
            var m1 = CompareStringsRegex.Match(s1);
            var m2 = CompareStringsRegex.Match(s2);
            string h1 = m1.Groups[1].Value;
            string h2 = m2.Groups[1].Value;
            if (h1 != h2 && !(string.IsNullOrEmpty(h1) || string.IsNullOrEmpty(h2)))
                return s1.CompareTo(s2);
            string t1 = m1.Groups[2].Value;
            string t2 = m2.Groups[2].Value;
            if (IsNumeric(t1) && IsNumeric(t2))
                return int.Parse(t1).CompareTo(int.Parse(t2));
            else if (!string.IsNullOrEmpty(t1) && !string.IsNullOrEmpty(t2))
                return t1.CompareTo(t2);
            else
                return s1.CompareTo(s2);
        } 

        #endregion
    }
    public class StringValueComparer : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            return Helper.CompareStrings(s1, s2);
        }
    }
}
