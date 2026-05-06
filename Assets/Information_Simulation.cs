// Une unité = 46400 km 
// Donc si la position de la Terre = (107.5, 0, 0) alors la distance entre la Terre et le Soleil est de 107.5 * 46400 km = 4 988 000 km

/* * --- TABLEAU DE RÉFÉRENCE ASTROPHYSIQUE (Échelle du projet) ---
 * * Unité de distance : 1 u = 1 391 609 km
 * Masse Solaire : 1.988e30 kg (Unity Mass = 1 000 000)
 * * +----------+----------+---------+---------+---------+---------+-------------+
 * | Planète  | Distance | Période | Vitesse | Gravité | Densité | Température |
 * |          | (Unity)  | (Jours) | (km/s)  | (m/s²)  | (g/cm³) | (Kelvin)    |
 * +----------+----------+---------+---------+---------+---------+-------------+
 * | Mercure  |   41.6   |   88.0  |   47.4  |   3.70  |   5.43  |     440     |
 * | Vénus    |   77.7   |  224.7  |   35.0  |   8.87  |   5.24  |     737     |
 * | Terre    |  107.5   |  365.25 |   29.8  |   9.81  |   5.51  |     288     |
 * | Mars     |  163.8   |  687.0  |   24.1  |   3.71  |   3.93  |     210     |
 * | Jupiter  |  559.4   |  4 332  |   13.1  |  24.79  |   1.33  |     165     |
 * | Saturne  |  1029.0  | 10 759  |    9.7  |  10.44  |   0.69  |     134     |
 * | Uranus   |  2060.3  | 30 688  |    6.8  |   8.69  |   1.27  |      76     |
 * | Neptune  |  3244.4  | 60 182  |    5.4  |  11.15  |   1.64  |      72     |
 * +----------+----------+---------+---------+---------+---------+-------------+
 */
 
 /*
  * Points clés de la troisième loi de Kepler :
  * Formule : \(T^2 / a^3 = k\) (avec \(k\) une constante).
  * Signification : Plus une planète est éloignée de l'astre autour duquel elle gravite (grand \(a\)), plus sa période de révolution (temps pour faire un tour) est longue (\(T\)).
  * Constante : Cette constante (\(k\)) est la même pour tous les corps en orbite autour du même astre attracteur (ex: le Soleil).
  * Démonstration Newtonienne : Newton a montré que cette constante vaut \(\frac{4\pi ^{2}}{GM}\), où \(G\) est la constante gravitationnelle et \(M\) la masse de l'astre central.
  * Unités SI : La période \(T\) est en secondes (\(s\)) et le demi-grand axe \(a\) en mètres (\(m\)
  */
  
  // L'énergie orbitale est la somme constante de l'énergie cinétique et de l'énergie potentielle gravitationnelle d'un corps en orbite