﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using GhSA.Util;
using Rhino.Geometry;

namespace GhSA.Util.Gsa
{
    /// <summary>
    /// Profile class holds information about a profile
    /// Type: Standard, Catalogue or Geometric
    /// ShapeOptions for Standard type
    /// Section units
    /// </summary>
    public class GsaProfile
    {
        public enum profileTypes
        {
            [Description("Standard")] Standard,
            [Description("Catalogue")] Catalogue,
            [Description("Geometric")] Geometric
        }
        public enum geoTypes
        {
            [Description("Perimeter")] Perim
            //[Description("Thin Wall")] ThinWall, // removed temporarily as not currently implemented (ADSEC-563)
            //[Description("Point")] Point         // removed temporarily as not currently implemented (ADSEC-563)
        }

        public enum stdShapeOptions
        {
            [Description("Rectangle")] Rectangle,
            [Description("Circle")] Circle,
            [Description("I section")] I_section,
            [Description("Tee")] Tee,
            [Description("Channel")] Channel,
            [Description("Angle")] Angle,
        }

        public enum sectUnitOptions
        {
            [Description("mm")] u_mm,
            [Description("cm")] u_cm,
            [Description("m")] u_m,
            [Description("ft")] u_ft,
            [Description("in")] u_in,
        }

        public profileTypes profileType;
        public stdShapeOptions stdShape;
        public geoTypes geoType;
        public sectUnitOptions sectUnit = sectUnitOptions.u_mm;

        public int catalogueIndex;
        public int catalogueTypeIndex;
        public int catalogueProfileIndex;

        public bool isTapered;
        public bool isHollow;
        public bool isElliptical;
        public bool isGeneral;
        public bool isB2B;

        public double d;
        public double b1;
        public double b2;
        public double tf1;
        public double tf2;
        public double tw1;
        public double tw2;

        public List<Point2d> perimeterPoints;
        public List<List<Point2d>> voidPoints;
    }
    /// <summary>
    /// Helper class for Profile/Section conversions
    /// </summary>
    public class ConvertSection
    {
        /// <summary>
        /// Method to update section units. 
        /// Use "factorValues" to automatically factor existing values to match new unit.
        /// </summary>
        /// <param name="gsaProfile"></param>
        /// <param name="factorValues"></param>
        /// <returns></returns>
        public static GsaProfile UpdateSectUnit(GsaProfile gsaProfile, bool factorValues)
        {
            if (gsaProfile.sectUnit.ToString() != Unit.Length_Section)
            {
                if (Unit.Length_Section == "mm" || Unit.Length_Section == "cm" || Unit.Length_Section == "m" ||
                Unit.Length_Section == "ft" || Unit.Length_Section == "in")
                {
                    
                    if (factorValues)
                    {
                        double conversionFactor = 1;
                        // convert current unit back to meters, I know that one
                        double toMeters = 1;
                        switch (Unit.Length_Section)
                        {
                            case "mm":
                                toMeters = 1/1000;
                                break;
                            case "cm":
                                toMeters = 1/100;
                                break;
                            case "m":
                                toMeters = 1;
                                break;
                            case "in":
                                toMeters = 1/(1000 / 25.4);
                                break;
                            case "ft":
                                toMeters = 1/(1000 / (12 * 25.4));
                                break;
                        }
                        // convert to new unit
                        switch (gsaProfile.sectUnit.ToString())
                        {
                            case "mm":
                                conversionFactor = toMeters * 1000;
                                break;
                            case "cm":
                                conversionFactor = toMeters * 100;
                                break;
                            case "m":
                                conversionFactor = toMeters * 1;
                                break;
                            case "in":
                                conversionFactor = toMeters * 1000 / 25.4;
                                break;
                            case "ft":
                                conversionFactor = toMeters * 1000 / (12 * 25.4);
                                break;
                        }
                        gsaProfile.d = gsaProfile.d * conversionFactor;
                        gsaProfile.b1 = gsaProfile.b1 * conversionFactor;
                        gsaProfile.b2 = gsaProfile.b2 * conversionFactor;
                        gsaProfile.tf1 = gsaProfile.tf1 * conversionFactor;
                        gsaProfile.tf2 = gsaProfile.tf2 * conversionFactor;
                        gsaProfile.tw1 = gsaProfile.tw1 * conversionFactor;
                        gsaProfile.tw2 = gsaProfile.tw2 * conversionFactor;
                    }
                        
                    switch (Unit.Length_Section)
                    {
                        case "mm":
                            gsaProfile.sectUnit = GsaProfile.sectUnitOptions.u_mm;
                            break;
                        case "cm":
                            gsaProfile.sectUnit = GsaProfile.sectUnitOptions.u_cm;
                            break;
                        case "m":
                            gsaProfile.sectUnit = GsaProfile.sectUnitOptions.u_m;
                            break;
                        case "in":
                            gsaProfile.sectUnit = GsaProfile.sectUnitOptions.u_in;
                            break;
                        case "ft":
                            gsaProfile.sectUnit = GsaProfile.sectUnitOptions.u_ft;
                            break;
                    }
                }
            }
            return gsaProfile;
        }
        /// <summary>
        /// Method to convert a GsaProfile to a string that can be read by GSA
        /// (in GsaAPI.Section.Profile or GhSA.Parameters.GsaSection.Section.Profile)
        /// 
        /// NOTE: 
        /// - Does not cover all profile types available in GSA (but all available in GsaProfile)
        /// - Geometric can handle custom profiles with voids. Origin/anchor to be implemented.
        /// - Catalogue profiles yet to be implemented
        /// </summary>
        /// <param name="gsaProfile"></param>
        /// <returns></returns>
        public static string ProfileConversion(GsaProfile gsaProfile)
        {
            if (gsaProfile.profileType == GsaProfile.profileTypes.Standard)
            {
                string unit = " ";
                if (gsaProfile.sectUnit != GsaProfile.sectUnitOptions.u_mm)
                    unit = "(" + gsaProfile.sectUnit.ToString() + ") ";

                if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.Rectangle)
                {
                    if (gsaProfile.isTapered)
                    {
                        return "STD TR" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.b2.ToString("0.############");
                    }
                    else
                    {
                        if (gsaProfile.isHollow)
                        {
                            return "STD RHS" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                        }
                        else
                        {
                            return "STD R" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############");
                        }
                    }
                }
                else if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.Circle)
                {
                    if (gsaProfile.isHollow)
                    {
                        if (gsaProfile.isElliptical)
                        {
                            return "STD OVAL" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############");
                        }
                        else
                        {
                            return "STD CHS" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############");
                        }
                    }
                    else
                    {
                        if (gsaProfile.isElliptical)
                        {
                            return "STD E" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " 2";
                        }
                        else
                        {
                            return "STD C" + unit + gsaProfile.d.ToString("0.############");
                        }
                    }
                }
                else if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.I_section)
                {
                    if (gsaProfile.isGeneral)
                    {
                        if (gsaProfile.isTapered)
                        {
                            return "STD TI" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.b2.ToString("0.############") + " "
                                + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tw2.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############") + " " + gsaProfile.tf2.ToString("0.############");
                        }
                        else
                        {
                            return "STD GI" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.b2.ToString("0.############") + " "
                                + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############") + " " + gsaProfile.tf2.ToString("0.############");
                        }
                    }
                    else
                    {
                        return "STD I" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                }
                else if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.Tee)
                {
                    if (gsaProfile.isTapered)
                    {
                        return "STD TT" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " "
                            + gsaProfile.tw2.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                    else
                    {
                        return "STD T" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                }

                else if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.Channel)
                {
                    if (gsaProfile.isB2B)
                    {
                        return "STD DCH" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                    else
                    {
                        return "STD CH" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                }

                else if (gsaProfile.stdShape == GsaProfile.stdShapeOptions.Angle)
                {
                    if (gsaProfile.isB2B)
                    {
                        return "STD D" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                    else
                    {
                        return "STD A" + unit + gsaProfile.d.ToString("0.############") + " " + gsaProfile.b1.ToString("0.############") + " " + gsaProfile.tw1.ToString("0.############") + " " + gsaProfile.tf1.ToString("0.############");
                    }
                }
                else
                {
                    return "STD something else";
                }
            }
            else if (gsaProfile.profileType == GsaProfile.profileTypes.Catalogue)
            {
                //let catalogueName = gsaProfile.catalogueNames[gsaProfile.catalogueIndex]
                //let typeName = gsaProfile.catalogueTypes(catalogueName)[gsaProfile.catalogueTypeIndex]
                //let typeAbbrev = gsaProfile.catalogueTypeAbbrev(catalogueName, typeName)
                //return "CAT " + typeAbbrev + " " + gsaProfile.sectionNames(catalogueName, typeName)[gsaProfile.catalogueProfileIndex]
                return "CAT"; //to be implemented
            }
            else if (gsaProfile.profileType == GsaProfile.profileTypes.Geometric)
            {
                if (gsaProfile.geoType == GsaProfile.geoTypes.Perim)
                {
                    string unit = "";
                    if (gsaProfile.sectUnit != GsaProfile.sectUnitOptions.u_mm)
                        unit = "(" + gsaProfile.sectUnit.ToString() + ") ";

                    var profile = "GEO P" + unit;
                    var iPoint = 0;
                    foreach (Point2d point in gsaProfile.perimeterPoints)
                    {
                        if ((iPoint > 0))
                            profile += " L";
                        else
                            profile += " M";

                        profile = profile + ("("
                                    + (point.X + ("|"
                                    + (point.Y + ")"))));
                        iPoint++;
                    }
                    if (!(gsaProfile.voidPoints == null || !(gsaProfile.voidPoints.Count > 0)))
                    {
                        for (int i = 0; i < gsaProfile.voidPoints.Count; i++)
                        {
                            iPoint = 0;
                            foreach (Point2d point in gsaProfile.voidPoints[i])
                            {
                                if ((iPoint > 0))
                                    profile += " L";
                                else
                                    profile += " M";

                                profile = profile + ("("
                                            + (point.X + ("|"
                                            + (point.Y + ")"))));
                                iPoint++;
                            }
                        }
                    }

                    return profile;
                }
                return "GEO";
            }
            else
                return null;
        }
    }
}
