using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System;
using UnityEngine.UI;


namespace MagicLeap
{
    public class UpdateCircleColor : MonoBehaviour
    {
        //The Color to be assigned to the Renderer’s Material
        private Color m_NewColor;
        SpriteRenderer m_SpriteRenderer;


        void Start()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
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

        void Update()
        {
            UpdateUI _UpdateUI = transform.parent.gameObject.GetComponent<UpdateUI>();

            //Set the Color to the values gained from the Sliders
            //m_NewColor = GenerateColorFromRange(0f, 50f, _UpdateUI.currentTemperature);
            //m_NewColor = GetColorFromRedYellowGreenGradient(_UpdateUI.currentTemperature);

            Color[] col = { new Color(50, 255, 100), new Color(150, 150, 50), new Color(255, 0, 0) };
            m_NewColor = convert_to_rgb(10, 40, _UpdateUI.currentTemperature, col);
            //Set the SpriteRenderer to the Color defined by the Sliders
            m_SpriteRenderer.color = m_NewColor;
        }
    }
}