using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System;
using UnityEngine.UI;


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


        private Color getColorFromTemperature(float temperature)
        {
            if(temperature < 10)
            {
                return new Color(0.560f,0.874f,1f);//#8fdfff
            }
            else if (temperature>=10 && temperature <12)
            {
                return new Color(0.560f, 1f, 0.996f);//#8ffffe
            }
            else if(temperature >= 12 && temperature < 14)
            {
                return new Color(0.560f, 1f, 0.921f);//#8fffeb
            }
            else if (temperature >= 14 && temperature < 16)
            {
                return new Color(0.560f, 1f, 0.878f);//#8fffe0
            }
            else if (temperature >= 16 && temperature < 18)
            {
                return new Color(0.560f, 1f, 0.776f);//#8fffc6
            }
            else if (temperature >= 18 && temperature < 19)
            {
                return new Color(0.560f, 1f, 0.643f);//#8fffa4
            }
            else if (temperature >= 19 && temperature < 20)
            {
                return new Color(0.560f, 1f, 0.564f);//#8fff90
            }
            else if (temperature >= 20 && temperature < 21)
            {
                return new Color(0.560f, 1f, 0.560f);//#9eff8f
            }
            else if (temperature >= 21 && temperature < 22)
            {
                return new Color(0.705f, 1f, 0.560f);//#b4ff8f
            }
            else if (temperature >= 22 && temperature < 22.5)
            {
                return new Color(0.752f, 1f, 0.560f);//#c0ff8f
            }
            else if (temperature >= 22.5 && temperature < 23)
            {
                return new Color(0.819f, 1f, 0.560f);//#d1ff8f
            }
            else if (temperature >= 23 && temperature < 23.5)
            {
                return new Color(0.878f, 1f, 0.560f);//#e0ff8f
            }
            else if (temperature >= 23.5 && temperature < 24)
            {
                return new Color(0.894f, 0.905f, 0.494f);//#e4e77e
            }
            else if (temperature >= 23.5 && temperature < 24)
            {
                return new Color(0.909f, 0.858f, 0.470f);//#e8db78
            }
            else if (temperature >= 24 && temperature < 24.25)
            {
                return new Color(0.909f, 0.823f, 0.470f);//#e8d278
            }
            else if (temperature >= 24.25 && temperature < 24.5)
            {
                return new Color(0.909f, 0.788f, 0.470f);//#e8c978
            }
            else if (temperature >= 24.5 && temperature < 24.75)
            {
                return new Color(0.909f, 0.745f, 0.470f);//#e8be78
            }
            else if (temperature >= 24.75 && temperature < 25)
            {
                return new Color(0.870f, 0.670f, 0.329f);//#deab54
            }
            else if (temperature >= 25 && temperature < 25.25)
            {
                return new Color(0.815f, 0.568f, 0.243f);//#d0913e
            }
            else if (temperature >= 25.25 && temperature < 25.5)
            {
                return new Color(0.792f, 0.537f, 0.286f);//#ca8949
            }
            else if (temperature >= 25.5 && temperature < 25.75)
            {
                return new Color(0.792f, 0.513f, 0.286f);//#ca8349
            }
            else if (temperature >= 25.75 && temperature < 26)
            {
                return new Color(0.831f, 0.576f, 0.431f);//#d4936e
            }
            else if (temperature >= 26 && temperature < 26.5)
            {
                return new Color(0.835f, 0.525f, 0.345f);//#d58658
            }
            else if (temperature >= 26.5 && temperature < 27)
            {
                return new Color(0.803f, 0.447f, 0.235f);//#cd723c
            }
            else if (temperature >= 27 && temperature < 27.5)
            {
                return new Color(0.803f, 0.431f, 0.235f);//#cd6e3c
            }
            else if (temperature >= 27.5 && temperature < 28)
            {
                return new Color(0.741f, 0.372f, 0.180f);//#bd5f2e
            }
            else if (temperature >= 28 && temperature < 28.5)
            {
                return new Color(0.705f, 1f, 0.560f);//#ce6d4b
            }
            else if (temperature >= 28.5 && temperature < 29)
            {
                return new Color(0.807f, 0.407f, 0.294f);//#ce684b
            }
            else if (temperature >= 29 && temperature < 29.5)
            {
                return new Color(0.874f, 0.360f, 0.227f);//#df5c3a
            }
            else if (temperature >= 29.5 && temperature < 30)
            {
                return new Color(0.874f, 0.321f, 0.227f);//#df523a
            }
            else if (temperature >= 30 && temperature < 31)
            {
                return new Color(0.909f, 0.278f, 0.172f);//#e8472c
            }
            else if (temperature >= 31 && temperature < 32)
            {
                return new Color(0.921f, 0.254f, 0.414f);//#eb4124
            }
            else if (temperature >= 32 && temperature < 33)
            {
                return new Color(0.937f, 0.239f, 0.121f);//#ef3d1f
            }
            else if (temperature >= 33 && temperature < 34)
            {
                return new Color(0.968f, 0.270f, 0.149f);//#f74526
            }
            else if (temperature >= 34 && temperature < 35)
            {
                return new Color(1f, 0.196f, 0.058f);//#ff320f
            }
            else if (temperature >= 35 && temperature < 36)
            {
                return new Color(0.874f, 0.133f, 0.003f);//#df2201
            }
            else if (temperature >= 36 && temperature < 37)
            {
                return new Color(0.819f, 0.121f, 0f);//#d11f00
            }
            else if (temperature >= 37 && temperature < 38)
            {
                return new Color(0.839f, 0.058f, 0f);//#d60f00
            }
            else if (temperature >= 38 && temperature < 39)
            {
                return new Color(0.717f, 0.050f, 0.003f);//#b70d01
            }
            else if (temperature >= 39 && temperature < 40)
            {
                return new Color(0.580f, 0.039f, 0f);//#940a00
            }
            else if (temperature >= 40 && temperature < 45)
            {
                return new Color(0.458f, 0.035f, 0.003f);//#750901
            }
            else if (temperature >= 45)
            {
                return new Color(0.0349f, 0.054f, 0.031f);//#590e08
            }
            else
            {
                return new Color(1f, 0.6f, 0.968f);//#ff99f7 //error
            }

        }

        private Color getColorFromTemperaturev2(float temperature)
        {
            if (temperature < 10)
            {
                return new Color(1f, 0.874f, 1f);//#8fdfff
            }
            else if (temperature >= 10 && temperature < 12)
            {
                return new Color(1f, 1f, 0.996f);//#8ffffe
            }
            else if (temperature >= 12 && temperature < 14)
            {
                return new Color(1f, 1f, 0.921f);//#8fffeb
            }
            else if (temperature >= 14 && temperature < 16)
            {
                return new Color(1f, 1f, 0.878f);//#8fffe0
            }
            else if (temperature >= 16 && temperature < 18)
            {
                return new Color(1f, 1f, 0.776f);//#8fffc6
            }
            else if (temperature >= 18 && temperature < 19)
            {
                return new Color(1f, 1f, 0.643f);//#8fffa4
            }
            else if (temperature >= 19 && temperature < 20)
            {
                return new Color(1f, 1f, 0.564f);//#8fff90
            }
            else if (temperature >= 20 && temperature < 21)
            {
                return new Color(1f, 1f, 0.560f);//#9eff8f
            }
            else if (temperature >= 21 && temperature < 22)
            {
                return new Color(1f, 1f, 0.560f);//#b4ff8f
            }
            else if (temperature >= 22 && temperature < 22.5)
            {
                return new Color(1f, 1f, 0.560f);//#c0ff8f
            }
            else if (temperature >= 22.5 && temperature < 23)
            {
                return new Color(1f, 1f, 0.560f);//#d1ff8f
            }
            else if (temperature >= 23 && temperature < 23.5)
            {
                return new Color(1f, 1f, 0.560f);//#e0ff8f
            }
            else if (temperature >= 23.5 && temperature < 24)
            {
                return new Color(1f, 0.905f, 0.494f);//#e4e77e
            }
            else if (temperature >= 23.5 && temperature < 24)
            {
                return new Color(1f, 0.858f, 0.470f);//#e8db78
            }
            else if (temperature >= 24 && temperature < 24.25)
            {
                return new Color(1f, 0.823f, 0.470f);//#e8d278
            }
            else if (temperature >= 24.25 && temperature < 24.5)
            {
                return new Color(1f, 0.788f, 0.470f);//#e8c978
            }
            else if (temperature >= 24.5 && temperature < 24.75)
            {
                return new Color(1f, 0.745f, 0.470f);//#e8be78
            }
            else if (temperature >= 24.75 && temperature < 25)
            {
                return new Color(1f, 0.670f, 0.329f);//#deab54
            }
            else if (temperature >= 25 && temperature < 25.25)
            {
                return new Color(1f, 0.568f, 0.243f);//#d0913e
            }
            else if (temperature >= 25.25 && temperature < 25.5)
            {
                return new Color(1f, 0.537f, 0.286f);//#ca8949
            }
            else if (temperature >= 25.5 && temperature < 25.75)
            {
                return new Color(1f, 0.513f, 0.286f);//#ca8349
            }
            else if (temperature >= 25.75 && temperature < 26)
            {
                return new Color(1f, 0.576f, 0.431f);//#d4936e
            }
            else if (temperature >= 26 && temperature < 26.5)
            {
                return new Color(1f, 0.525f, 0.345f);//#d58658
            }
            else if (temperature >= 26.5 && temperature < 27)
            {
                return new Color(1f, 0.447f, 0.235f);//#cd723c
            }
            else if (temperature >= 27 && temperature < 27.5)
            {
                return new Color(1f, 0.431f, 0.235f);//#cd6e3c
            }
            else if (temperature >= 27.5 && temperature < 28)
            {
                return new Color(1f, 0.372f, 0.180f);//#bd5f2e
            }
            else if (temperature >= 28 && temperature < 28.5)
            {
                return new Color(1f, 1f, 0.560f);//#ce6d4b
            }
            else if (temperature >= 28.5 && temperature < 29)
            {
                return new Color(1f, 0.407f, 0.294f);//#ce684b
            }
            else if (temperature >= 29 && temperature < 29.5)
            {
                return new Color(1f, 0.360f, 0.227f);//#df5c3a
            }
            else if (temperature >= 29.5 && temperature < 30)
            {
                return new Color(1f, 0.321f, 0.227f);//#df523a
            }
            else if (temperature >= 30 && temperature < 31)
            {
                return new Color(1f, 0.278f, 0.172f);//#e8472c
            }
            else if (temperature >= 31 && temperature < 32)
            {
                return new Color(1f, 0.254f, 0.414f);//#eb4124
            }
            else if (temperature >= 32 && temperature < 33)
            {
                return new Color(1f, 0.239f, 0.121f);//#ef3d1f
            }
            else if (temperature >= 33 && temperature < 34)
            {
                return new Color(1f, 0.270f, 0.149f);//#f74526
            }
            else if (temperature >= 34 && temperature < 35)
            {
                return new Color(1f, 0.196f, 0.058f);//#ff320f
            }
            else if (temperature >= 35 && temperature < 36)
            {
                return new Color(1f, 0.133f, 0.003f);//#df2201
            }
            else if (temperature >= 36 && temperature < 37)
            {
                return new Color(1f, 0.121f, 0f);//#d11f00
            }
            else if (temperature >= 37 && temperature < 38)
            {
                return new Color(1f, 0.058f, 0f);//#d60f00
            }
            else if (temperature >= 38 && temperature < 39)
            {
                return new Color(1f, 0.050f, 0.003f);//#b70d01
            }
            else if (temperature >= 39 && temperature < 40)
            {
                return new Color(1f, 0.039f, 0f);//#940a00
            }
            else if (temperature >= 40 && temperature < 45)
            {
                return new Color(1f, 0.035f, 0.003f);//#750901
            }
            else if (temperature >= 45)
            {
                return new Color(1f, 0.054f, 0.031f);//#590e08
            }
            else
            {
                return new Color(1f, 0.6f, 0.968f);//#ff99f7 //error
            }

        }


        void Update()
        {
#if false
            GameObject _MeshingNodes = GameObject.Find("MeshingNodes");
            Control _Control = _MeshingNodes.GetComponent<Control>();

            if (_Control.areParticlesActive == true)
            {
                ps.gameObject.SetActive(true);
            }
            else
            {
                ps.gameObject.SetActive(false);
            }
#endif
            UpdateUI _UpdateUI = transform.parent.gameObject.GetComponent<UpdateUI>();

            //Set the Color to the values gained from the Sliders
            //m_NewColor = GenerateColorFromRange(0f, 50f, _UpdateUI.currentTemperature);
            //m_NewColor = GetColorFromRedYellowGreenGradient(_UpdateUI.currentTemperature);

            Color[] colorRange = { new Color(50, 255, 100), new Color(100, 150, 50), new Color(150, 0, 0) };
            m_NewColor = convert_to_rgb(19  ,40, _UpdateUI.currentTemperature, colorRange);

            //m_NewColor = getColorFromTemperaturev2(_UpdateUI.currentTemperature);
            if (_UpdateUI.currentTemperature == 0)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }
            else
            {
                var emission = ps.emission;
                emission.enabled = true;

                var col = ps.colorOverLifetime;
                col.enabled = true;

                Gradient grad = new Gradient();
                grad.SetKeys(new GradientColorKey[] { new GradientColorKey(m_NewColor, 0.0f), new GradientColorKey(m_NewColor, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.10f), new GradientAlphaKey(1.0f, 0.9f), new GradientAlphaKey(0.0f, 1.0f) });

                col.color = grad;
            }


        }
    }
}