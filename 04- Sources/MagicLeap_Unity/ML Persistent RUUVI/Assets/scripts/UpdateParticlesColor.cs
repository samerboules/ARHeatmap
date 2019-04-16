using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System;
using UnityEngine.UI;
using System.IO;

namespace MagicLeap
{
    public class UpdateParticlesColor : MonoBehaviour
    {
        //The Color to be assigned to the Renderer’s Material
        private Color m_NewColor;
        ParticleSystem ps;


        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            //ps.gameObject.SetActive(false);
        }

        Color GenerateColorFromRange(float minimum, float maximum, float value)
        {
            float ratio = (2 * (value - minimum)) / (maximum - minimum);
            float b = Math.Max(0f, 255f * (1f - ratio));
            float g = Math.Max(0, 255 * (ratio - 1));
            float r = 255f - 2 * b - 2 * g;
            return new Color(r, g, b);
        }

        Color GetColorFromRedYellowGreenGradient(double temperatureInCelcius)
        {
            double percentage = (temperatureInCelcius / 70) * 100;
            var red = (percentage > 50 ? 1 - 2 * (percentage - 50) / 100.0 : 1.0) * 255;
            var green = (percentage > 50 ? 1.0 : 2 * percentage / 100.0) * 255;
            var blue = 0.0;
            Color result = new Color((float)red, (float)green, (float)blue);
            return result;
        }


        /*
         * https://stackoverflow.com/a/20793850
         * https://www.rapidtables.com/web/color/RGB_Color.html
        */
        Color convert_to_rgb(double minval, double maxval, double val, Color[] colors)
        {
            /*
            # "colors" is a series of RGB colors delineating a series of
            # adjacent linear color gradients between each pair.
            # Determine where the given value falls proportionality within
            # the range from minval->maxval and scale that fractional value
            # by the total number in the "colors" pallette.
            */
            double i_f = (double)((val - minval) / (maxval - minval) * ((colors.Length) - 1));

            /*
            # Determine the lower index of the pair of color indices this
            # value corresponds and its fractional distance between the lower
            # and the upper colors.
            */
            //# Split into whole & fractional parts.
            int i = (int)(i_f / 1);
            double f = i_f % 1;

            //# Does it fall exactly on one of the color points?
            if (f < (2.22045e-16))
            {
                return colors[i];
            }
            else //# Otherwise return a color within the range between them.
            {
                float r1 = colors[i].r;
                float g1 = colors[i].g;
                float b1 = colors[i].b;
                float r2 = colors[i + 1].r;
                float g2 = colors[i + 1].g;
                float b2 = colors[i + 1].b;
                Color result = new Color((float)((r1 + f * (r2 - r1)) / 255f), (float)((g1 + f * (g2 - g1)) / 255f), (float)((b1 + f * (b2 - b1))) / 255f);
                return result;
            }
        }


        //From Joris Image (Temperature range from 15 to 50)
        //https://answers.unity.com/questions/213737/best-and-simplest-way-to-read-color-from-a-texture.html
        private Color GetColorFromTexture(float temperature)
        {
            if (temperature < 15f)
            {
                return new Color(0.482f, 0.745f, 0.937f, 1.00f);
            }
            else if (temperature > 50f)
            {
                return new Color(0.906f, 0.380f, 0.937f, 1.000f);
            }
            else //Range 15 -> 50
            {
                Color[] m_Terrain;
                int m_TerrainWidth;
                int m_TerrainHeight;

                Texture2D t = Resources.Load("TempRange", typeof(Texture2D)) as Texture2D;
                m_Terrain = t.GetPixels();
                m_TerrainWidth = t.width;
                m_TerrainHeight = t.height;

                //Equation of straght line y=mx+b //http://www.matrixlab-examples.com/equation-of-a-straight-line.html
                float w = (7.31f) * temperature + (-109.71f);

                //Color Test //http://doc.instantreality.org/tools/color_calculator/
                Color c = m_Terrain[5 * m_TerrainWidth + (int)w];
                return c;
            }
        }

    void Update()
        {
            //Get UpdateUI script to read the current temperature
            UpdateUI _UpdateUI = transform.parent.gameObject.GetComponent<UpdateUI>();

            //Get color from pixel
            m_NewColor = GetColorFromTexture(_UpdateUI.currentTemperature);

            var emission = ps.emission;
            emission.enabled = true;

            var col = ps.colorOverLifetime;
            col.enabled = true;

            //Gradient grad = new Gradient();
            //grad.SetKeys(new GradientColorKey[] { new GradientColorKey(m_NewColor, 0.0f), new GradientColorKey(m_NewColor, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.10f), new GradientAlphaKey(1.0f, 0.9f), new GradientAlphaKey(0.0f, 1.0f) });

            col.color = m_NewColor;
        }
    }
}