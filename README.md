MKS Explainer
===

This Kerbal Space Program mod adds in game explanations about efficiency/load calculations done by MKS.
It mostly shows intermediate steps, so that you can determine where this "843.52% load" is coming from (and how to improve it).

[Spacedock page (for direct download)](https://spacedock.info/mod/1253/MKS%20Explainer)

[KSP forum release thread](http://forum.kerbalspaceprogram.com/index.php?/topic/157755-12-mks-explainer/)

[Screenshot](http://i.imgur.com/uaxVOO9h.png)



How to build (Linux):
---------------------

- have KSP installed with at least MKS and USI Life Support
- install xbuild, mono-devel, mono-reference-assemblies-3.5, and their dependencies
- create a symlink at the root of the repository, toward your KSP install, called KSP_INSTALL
    Ex: `ln -s ~/.steam/steam/steamapps/common/Kerbal\ Space\ Program KSP_INSTALL`
- run:
    `xbuild`
